using System;
using System.Collections.Generic;
using System.Numerics;

namespace Lockstep.Nav
{
    /// <summary>
    /// 纯定点、与 Unity Navigation 无关的高度场导航网格生成器。
    /// <para>流程为：过滤可行走三角形、栅格化多层高度场、检查垂直净空与步高、
    /// 按代理半径腐蚀边界、移除微小区域、合并连续共面单元、三角化并建立邻接和离线链接。</para>
    /// <para>生成器面向地面型导航。它支持坡道、台阶和上下叠层，但不生成垂直墙面或任意重力方向表面。</para>
    /// </summary>
    public static class NavMeshBuilder
    {
        private struct WalkableTriangle
        {
            public LVector3 a;
            public LVector3 b;
            public LVector3 c;
            public LFloat denominator;
            public LFloat minX;
            public LFloat maxX;
            public LFloat minZ;
            public LFloat maxZ;
            /// <summary>
            /// 未归一化的原始平面法线。栅格化之后仍保留它，是为了区分“同一斜面上的不同高度”
            /// 与“高度相近但属于不同台阶/坡面”这两种情况，避免仅比较采样高度造成错误合并。
            /// </summary>
            public LVector3 normal;

            public bool ContainsXZ(LFloat x, LFloat z, out LFloat height)
            {
                LFloat u =
                    ((b.z - c.z) * (x - c.x) +
                     (c.x - b.x) * (z - c.z)) / denominator;
                LFloat v =
                    ((c.z - a.z) * (x - c.x) +
                     (a.x - c.x) * (z - c.z)) / denominator;
                LFloat w = LFloat.one - u - v;
                if (u < -NavHelper.epsilon || v < -NavHelper.epsilon || w < -NavHelper.epsilon)
                {
                    height = LFloat.zero;
                    return false;
                }

                height = a.y * u + b.y * v + c.y * w;
                return true;
            }
        }

        private sealed class Span
        {
            public int x;
            public int z;
            public LFloat height;
            public bool walkable;
            public readonly Span[] neighbors = new Span[4];
            public int region;
            /// <summary>产生当前高度样本的原始三角形平面上的任意一点。</summary>
            public LVector3 sourcePlanePoint;

            /// <summary>产生当前高度样本的原始三角形平面法线，未归一化。</summary>
            public LVector3 sourcePlaneNormal;

            /// <summary>单元中心的实际采样点，用于判断相邻单元是否仍落在同一平面。</summary>
            public LVector3 samplePoint;
        }

        /// <summary>一个可行走单元最终四个角点的几何，以及简化阶段的归属状态。</summary>
        private sealed class CellGeometry
        {
            public Span span;
            public LVector3 p00;
            public LVector3 p10;
            public LVector3 p11;
            public LVector3 p01;
            public bool assigned;
            public object patch;
            public readonly int[] boundaryTriangles = { -1, -1, -1, -1 };
        }

        /// <summary>由连续共面单元合并出的矩形块，每块只生成两个三角形。</summary>
        private sealed class RectanglePatch
        {
            public LVector3 p00;
            public LVector3 p10;
            public LVector3 p11;
            public LVector3 p01;
            public int northEastTriangle;
            public int southWestTriangle;
        }

        /// <summary>
        /// 一个共面高度场区域的约束德洛内结果。cells 用来恢复区域与外部过渡带之间的逐格 Portal；
        /// vertices/triangles 是尚未写入最终 NavData 的紧凑索引几何。
        /// </summary>
        private sealed class DelaunayPatch
        {
            public readonly List<CellGeometry> cells = new List<CellGeometry>();
            public readonly HashSet<Span> spans = new HashSet<Span>();
            public List<LVector3> vertices;
            public List<ConstrainedDelaunay.IndexTriangle> triangles;
            public readonly List<int> outputTriangles = new List<int>();
            public LVector3 planePoint;
            public LVector3 planeNormal;
        }

        /// <summary>用于把栅格轮廓小边快速映射回最终德洛内三角形的边索引项。</summary>
        private readonly struct BoundaryEdgeOwner
        {
            public readonly Edge edge;
            public readonly int triangle;

            public BoundaryEdgeOwner(Edge edge, int triangle)
            {
                this.edge = edge;
                this.triangle = triangle;
            }
        }

        /// <summary>
        /// 输入几何在一个高度场柱中的保守垂直占用区间。
        /// 水平地面退化为零厚度区间；闭合障碍的侧面会形成从底到顶的实体区间。
        /// </summary>
        private struct SolidInterval
        {
            public LFloat min;
            public LFloat max;
            public bool walkableSurface;
        }

        private readonly struct VertexKey : IEquatable<VertexKey>, IComparable<VertexKey>
        {
            private readonly long x;
            private readonly long y;
            private readonly long z;

            public VertexKey(LVector3 value)
            {
                x = value._x;
                y = value._y;
                z = value._z;
            }

            public bool Equals(VertexKey other) => x == other.x && y == other.y && z == other.z;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);

            public int CompareTo(VertexKey other)
            {
                int result = x.CompareTo(other.x);
                if (result != 0) return result;
                result = z.CompareTo(other.z);
                return result != 0 ? result : y.CompareTo(other.y);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = x.GetHashCode();
                    hash = hash * 397 ^ y.GetHashCode();
                    return hash * 397 ^ z.GetHashCode();
                }
            }
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            private readonly int a;
            private readonly int b;

            public EdgeKey(int a, int b)
            {
                if (a <= b)
                {
                    this.a = a;
                    this.b = b;
                }
                else
                {
                    this.a = b;
                    this.b = a;
                }
            }

            public bool Equals(EdgeKey other) => a == other.a && b == other.b;
            public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
            public override int GetHashCode() => unchecked(a * 397 ^ b);
        }

        private readonly struct EdgeOwner
        {
            public readonly int triangle;

            public EdgeOwner(int triangle)
            {
                this.triangle = triangle;
            }
        }

        private static readonly int[] NeighborX = { -1, 1, 0, 0 };
        private static readonly int[] NeighborZ = { 0, 0, -1, 1 };

        /// <summary>根据输入三角形与可选离线链接生成完整 NavData。</summary>
        public static NavData Build(
            IList<NavBuildTriangle> geometry,
            NavBuildSettings settings,
            IList<NavBuildLink> links,
            out NavBuildReport report)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();

            report = new NavBuildReport { inputTriangles = geometry.Count };
            List<WalkableTriangle> surfaces = CollectWalkableTriangles(geometry, settings);
            report.walkableInputTriangles = surfaces.Count;

            NavData nav = new NavData { agentType = settings.agentType };
            if (surfaces.Count == 0)
                return nav;

            LFloat minX = surfaces[0].minX;
            LFloat maxX = surfaces[0].maxX;
            LFloat minZ = surfaces[0].minZ;
            LFloat maxZ = surfaces[0].maxZ;
            for (int i = 1; i < surfaces.Count; i++)
            {
                minX = LMath.Min(minX, surfaces[i].minX);
                maxX = LMath.Max(maxX, surfaces[i].maxX);
                minZ = LMath.Min(minZ, surfaces[i].minZ);
                maxZ = LMath.Max(maxZ, surfaces[i].maxZ);
            }

            int width = LMath.Max(((maxX - minX) / settings.cellSize).Ceil(), 1);
            int depth = LMath.Max(((maxZ - minZ) / settings.cellSize).Ceil(), 1);
            long cellCount = (long)width * depth;
            if (cellCount > 16_000_000L)
                throw new InvalidOperationException(
                    $"Navigation heightfield would contain {cellCount} columns. Increase cellSize or split the source geometry.");

            List<Span>[] columns = new List<Span>[cellCount];
            List<SolidInterval>[] solids = new List<SolidInterval>[cellCount];
            RasterizeSolids(geometry, settings, minX, minZ, width, depth, solids);
            Rasterize(surfaces, settings, minX, minZ, width, depth, columns);
            MarkClearance(columns, solids, settings.agentHeight);
            ConnectNeighbors(columns, width, depth, settings.maxStepHeight);

            int erosionCells = settings.agentRadius <= LFloat.zero
                ? 0
                : (settings.agentRadius / settings.cellSize).Ceil();
            if (erosionCells > 0)
            {
                Erode(columns, width, depth, erosionCells);
                ConnectNeighbors(columns, width, depth, settings.maxStepHeight);
            }

            RemoveSmallRegions(columns, settings.minRegionCells);
            ConnectNeighbors(columns, width, depth, settings.maxStepHeight);

            List<Span> walkableSpans = CollectWalkableSpans(columns);
            report.rasterizedCells = CountSpans(columns);
            report.walkableCells = walkableSpans.Count;
            Triangulate(
                nav,
                walkableSpans,
                settings,
                minX,
                minZ,
                ref report);
            report.outputTriangles = nav.triangles.Count;
            AddLinks(nav, links, ref report);
            return nav;
        }

        public static NavData Build(
            IList<NavBuildTriangle> geometry,
            NavBuildSettings settings,
            out NavBuildReport report)
        {
            return Build(geometry, settings, null, out report);
        }

        private static List<WalkableTriangle> CollectWalkableTriangles(
            IList<NavBuildTriangle> geometry,
            NavBuildSettings settings)
        {
            var result = new List<WalkableTriangle>(geometry.Count);
            for (int i = 0; i < geometry.Count; i++)
            {
                NavBuildTriangle source = geometry[i];
                if (source.blockWalkableSurface) continue;
                LVector3 cross = LVector3.Cross(source.b - source.a, source.c - source.a);
                LFloat length = cross.magnitude;
                if (length <= LFloat.EPSILON) continue;

                // 输入索引绕序可能相反，因此坡度判断使用法线 Y 分量绝对值。
                LFloat normalY = LMath.Abs(cross.y) / length;
                if (normalY < settings.minWalkableNormalY) continue;

                LFloat denominator =
                    (source.b.z - source.c.z) * (source.a.x - source.c.x) +
                    (source.c.x - source.b.x) * (source.a.z - source.c.z);
                if (LMath.Abs(denominator) <= LFloat.EPSILON) continue;

                result.Add(new WalkableTriangle
                {
                    a = source.a,
                    b = source.b,
                    c = source.c,
                    denominator = denominator,
                    minX = LMath.Min(source.a.x, source.b.x, source.c.x),
                    maxX = LMath.Max(source.a.x, source.b.x, source.c.x),
                    minZ = LMath.Min(source.a.z, source.b.z, source.c.z),
                    maxZ = LMath.Max(source.a.z, source.b.z, source.c.z),
                    normal = cross
                });
            }
            return result;
        }

        private static void Rasterize(
            List<WalkableTriangle> surfaces,
            NavBuildSettings settings,
            LFloat originX,
            LFloat originZ,
            int width,
            int depth,
            List<Span>[] columns)
        {
            LFloat halfCell = settings.cellSize / 2;
            for (int triangleIndex = 0; triangleIndex < surfaces.Count; triangleIndex++)
            {
                WalkableTriangle triangle = surfaces[triangleIndex];
                int minCellX = LMath.Clamp(((triangle.minX - originX) / settings.cellSize).Floor(), 0, width - 1);
                int maxCellX = LMath.Clamp(((triangle.maxX - originX) / settings.cellSize).Floor(), 0, width - 1);
                int minCellZ = LMath.Clamp(((triangle.minZ - originZ) / settings.cellSize).Floor(), 0, depth - 1);
                int maxCellZ = LMath.Clamp(((triangle.maxZ - originZ) / settings.cellSize).Floor(), 0, depth - 1);

                for (int z = minCellZ; z <= maxCellZ; z++)
                {
                    LFloat sampleZ = originZ + settings.cellSize * z + halfCell;
                    for (int x = minCellX; x <= maxCellX; x++)
                    {
                        LFloat sampleX = originX + settings.cellSize * x + halfCell;
                        if (!triangle.ContainsXZ(sampleX, sampleZ, out LFloat height)) continue;

                        int columnIndex = z * width + x;
                        List<Span> column = columns[columnIndex];
                        if (column == null)
                        {
                            column = new List<Span>(1);
                            columns[columnIndex] = column;
                        }

                        InsertHeight(
                            column,
                            x,
                            z,
                            sampleX,
                            sampleZ,
                            height,
                            settings.maxStepHeight,
                            triangle);
                    }
                }
            }
        }

        private static void InsertHeight(
            List<Span> column,
            int x,
            int z,
            LFloat sampleX,
            LFloat sampleZ,
            LFloat height,
            LFloat mergeHeight,
            WalkableTriangle triangle)
        {
            int insertIndex = 0;
            while (insertIndex < column.Count && column[insertIndex].height < height)
                insertIndex++;

            if (insertIndex > 0 && LMath.Abs(column[insertIndex - 1].height - height) <= mergeHeight)
            {
                // 同一台阶内重叠面只保留较高样本，避免网格缝隙生成重复楼层。
                if (height >= column[insertIndex - 1].height)
                    SetSpanSurface(column[insertIndex - 1], sampleX, sampleZ, height, triangle);
                return;
            }
            if (insertIndex < column.Count && LMath.Abs(column[insertIndex].height - height) <= mergeHeight)
            {
                if (height >= column[insertIndex].height)
                    SetSpanSurface(column[insertIndex], sampleX, sampleZ, height, triangle);
                return;
            }

            var span = new Span { x = x, z = z, walkable = true };
            SetSpanSurface(span, sampleX, sampleZ, height, triangle);
            column.Insert(insertIndex, span);
        }

        private static void SetSpanSurface(
            Span span,
            LFloat sampleX,
            LFloat sampleZ,
            LFloat height,
            WalkableTriangle triangle)
        {
            span.height = height;
            span.samplePoint = new LVector3(sampleX, height, sampleZ);
            span.sourcePlanePoint = triangle.a;
            span.sourcePlaneNormal = triangle.normal;
        }

        private static void MarkClearance(
            List<Span>[] columns,
            List<SolidInterval>[] solids,
            LFloat agentHeight)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;

                List<SolidInterval> solidColumn = solids[i];
                for (int j = 0; j < column.Count; j++)
                {
                    Span span = column[j];
                    if (solidColumn == null) continue;

                    LFloat head = span.height + agentHeight;
                    for (int solidIndex = 0; solidIndex < solidColumn.Count; solidIndex++)
                    {
                        SolidInterval solid = solidColumn[solidIndex];
                        // 自身承载面以及完全位于脚下的实体不占用站立空间。
                        if (solid.max <= span.height + NavHelper.epsilon) continue;
                        if (solid.min >= head) break;

                        // 坡面在一个单元内通常存在小幅高度变化。只要当前脚底高度位于可行走
                        // 表面的局部区间中，就把该区间视为承载面，而不是误判为头顶障碍。
                        if (solid.walkableSurface &&
                            solid.min <= span.height + NavHelper.epsilon &&
                            solid.max >= span.height - NavHelper.epsilon)
                            continue;

                        span.walkable = false;
                        break;
                    }
                }
            }
        }

        private static void RasterizeSolids(
            IList<NavBuildTriangle> geometry,
            NavBuildSettings settings,
            LFloat originX,
            LFloat originZ,
            int width,
            int depth,
            List<SolidInterval>[] solids)
        {
            for (int triangleIndex = 0; triangleIndex < geometry.Count; triangleIndex++)
            {
                NavBuildTriangle triangle = geometry[triangleIndex];
                LFloat cellSize = settings.cellSize;
                LFloat minX = LMath.Min(triangle.a.x, triangle.b.x, triangle.c.x);
                LFloat maxX = LMath.Max(triangle.a.x, triangle.b.x, triangle.c.x);
                LFloat minZ = LMath.Min(triangle.a.z, triangle.b.z, triangle.c.z);
                LFloat maxZ = LMath.Max(triangle.a.z, triangle.b.z, triangle.c.z);
                int minCellX = LMath.Clamp(((minX - originX) / cellSize).Floor(), 0, width - 1);
                int maxCellX = LMath.Clamp(((maxX - originX) / cellSize).Floor(), 0, width - 1);
                int minCellZ = LMath.Clamp(((minZ - originZ) / cellSize).Floor(), 0, depth - 1);
                int maxCellZ = LMath.Clamp(((maxZ - originZ) / cellSize).Floor(), 0, depth - 1);
                bool walkableSurface = IsWalkableSurface(triangle, settings.minWalkableNormalY);

                for (int z = minCellZ; z <= maxCellZ; z++)
                {
                    LFloat cellMinZ = originZ + cellSize * z;
                    LFloat cellMaxZ = cellMinZ + cellSize;
                    for (int x = minCellX; x <= maxCellX; x++)
                    {
                        LFloat cellMinX = originX + cellSize * x;
                        LFloat cellMaxX = cellMinX + cellSize;
                        if (!TryGetCellHeightRange(
                                triangle,
                                cellMinX,
                                cellMaxX,
                                cellMinZ,
                                cellMaxZ,
                                out LFloat intervalMin,
                                out LFloat intervalMax))
                            continue;

                        var interval = new SolidInterval
                        {
                            min = intervalMin,
                            max = intervalMax,
                            walkableSurface = walkableSurface
                        };

                        int columnIndex = z * width + x;
                        List<SolidInterval> column = solids[columnIndex];
                        if (column == null)
                        {
                            column = new List<SolidInterval>(2);
                            solids[columnIndex] = column;
                        }
                        InsertSolidInterval(column, interval);
                    }
                }
            }
        }

        private static void InsertSolidInterval(List<SolidInterval> column, SolidInterval value)
        {
            int index = 0;
            while (index < column.Count && column[index].min <= value.min)
                index++;
            column.Insert(index, value);

            // 合并重叠实体区间，既减少后续净空判断次数，也把闭合网格的多个侧面组合成完整体积。
            int mergeIndex = LMath.Max(index - 1, 0);
            while (mergeIndex + 1 < column.Count)
            {
                SolidInterval current = column[mergeIndex];
                SolidInterval next = column[mergeIndex + 1];
                if (next.min > current.max + NavHelper.epsilon)
                {
                    mergeIndex++;
                    continue;
                }

                current.min = LMath.Min(current.min, next.min);
                current.max = LMath.Max(current.max, next.max);
                // 只要合并区间中混入墙体等不可行走面，整个区间就必须继续作为实体处理。
                current.walkableSurface &= next.walkableSurface;
                column[mergeIndex] = current;
                column.RemoveAt(mergeIndex + 1);
            }
        }

        private static bool IsWalkableSurface(
            NavBuildTriangle triangle,
            LFloat minWalkableNormalY)
        {
            LVector3 cross = LVector3.Cross(triangle.b - triangle.a, triangle.c - triangle.a);
            LFloat length = cross.magnitude;
            return !triangle.blockWalkableSurface &&
                   length > LFloat.EPSILON &&
                   LMath.Abs(cross.y) / length >= minWalkableNormalY;
        }

        /// <summary>
        /// 求三角形投影与当前 XZ 单元交集的局部高度范围。
        /// 交集多边形的极值只可能出现在三角形顶点、单元角点或边界交点，
        /// 因而无需分配临时多边形即可得到精确的线性高度上下界。
        /// </summary>
        private static bool TryGetCellHeightRange(
            NavBuildTriangle triangle,
            LFloat minX,
            LFloat maxX,
            LFloat minZ,
            LFloat maxZ,
            out LFloat minHeight,
            out LFloat maxHeight)
        {
            bool found = false;
            minHeight = LFloat.MaxValue;
            maxHeight = LFloat.MinValue;

            IncludeVertexInCell(triangle.a, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            IncludeVertexInCell(triangle.b, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            IncludeVertexInCell(triangle.c, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);

            IncludeCellCorner(triangle, minX, minZ, ref found, ref minHeight, ref maxHeight);
            IncludeCellCorner(triangle, maxX, minZ, ref found, ref minHeight, ref maxHeight);
            IncludeCellCorner(triangle, maxX, maxZ, ref found, ref minHeight, ref maxHeight);
            IncludeCellCorner(triangle, minX, maxZ, ref found, ref minHeight, ref maxHeight);

            IncludeEdgeIntersections(triangle.a, triangle.b, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            IncludeEdgeIntersections(triangle.b, triangle.c, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            IncludeEdgeIntersections(triangle.c, triangle.a, minX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            return found;
        }

        private static void IncludeVertexInCell(
            LVector3 vertex,
            LFloat minX,
            LFloat maxX,
            LFloat minZ,
            LFloat maxZ,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            if (vertex.x < minX || vertex.x > maxX || vertex.z < minZ || vertex.z > maxZ)
                return;
            IncludeHeight(vertex.y, ref found, ref minHeight, ref maxHeight);
        }

        private static void IncludeCellCorner(
            NavBuildTriangle triangle,
            LFloat x,
            LFloat z,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            LFloat denominator =
                (triangle.b.z - triangle.c.z) * (triangle.a.x - triangle.c.x) +
                (triangle.c.x - triangle.b.x) * (triangle.a.z - triangle.c.z);
            if (LMath.Abs(denominator) <= LFloat.EPSILON) return;

            LFloat u =
                ((triangle.b.z - triangle.c.z) * (x - triangle.c.x) +
                 (triangle.c.x - triangle.b.x) * (z - triangle.c.z)) / denominator;
            LFloat v =
                ((triangle.c.z - triangle.a.z) * (x - triangle.c.x) +
                 (triangle.a.x - triangle.c.x) * (z - triangle.c.z)) / denominator;
            LFloat w = LFloat.one - u - v;
            if (u < -NavHelper.epsilon || v < -NavHelper.epsilon || w < -NavHelper.epsilon)
                return;

            IncludeHeight(
                triangle.a.y * u + triangle.b.y * v + triangle.c.y * w,
                ref found,
                ref minHeight,
                ref maxHeight);
        }

        private static void IncludeEdgeIntersections(
            LVector3 a,
            LVector3 b,
            LFloat minX,
            LFloat maxX,
            LFloat minZ,
            LFloat maxZ,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            LFloat deltaX = b.x - a.x;
            if (LMath.Abs(deltaX) > LFloat.EPSILON)
            {
                IncludeEdgeAtX(a, b, deltaX, minX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
                IncludeEdgeAtX(a, b, deltaX, maxX, minZ, maxZ, ref found, ref minHeight, ref maxHeight);
            }

            LFloat deltaZ = b.z - a.z;
            if (LMath.Abs(deltaZ) > LFloat.EPSILON)
            {
                IncludeEdgeAtZ(a, b, deltaZ, minZ, minX, maxX, ref found, ref minHeight, ref maxHeight);
                IncludeEdgeAtZ(a, b, deltaZ, maxZ, minX, maxX, ref found, ref minHeight, ref maxHeight);
            }
        }

        private static void IncludeEdgeAtX(
            LVector3 a,
            LVector3 b,
            LFloat deltaX,
            LFloat x,
            LFloat minZ,
            LFloat maxZ,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            LFloat t = (x - a.x) / deltaX;
            if (t < LFloat.zero || t > LFloat.one) return;
            LFloat z = a.z + (b.z - a.z) * t;
            if (z < minZ || z > maxZ) return;
            IncludeHeight(a.y + (b.y - a.y) * t, ref found, ref minHeight, ref maxHeight);
        }

        private static void IncludeEdgeAtZ(
            LVector3 a,
            LVector3 b,
            LFloat deltaZ,
            LFloat z,
            LFloat minX,
            LFloat maxX,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            LFloat t = (z - a.z) / deltaZ;
            if (t < LFloat.zero || t > LFloat.one) return;
            LFloat x = a.x + (b.x - a.x) * t;
            if (x < minX || x > maxX) return;
            IncludeHeight(a.y + (b.y - a.y) * t, ref found, ref minHeight, ref maxHeight);
        }

        private static void IncludeHeight(
            LFloat height,
            ref bool found,
            ref LFloat minHeight,
            ref LFloat maxHeight)
        {
            if (!found)
            {
                found = true;
                minHeight = height;
                maxHeight = height;
                return;
            }
            minHeight = LMath.Min(minHeight, height);
            maxHeight = LMath.Max(maxHeight, height);
        }

        private static void ConnectNeighbors(
            List<Span>[] columns,
            int width,
            int depth,
            LFloat maxStepHeight)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                    Array.Clear(column[j].neighbors, 0, column[j].neighbors.Length);
            }

            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                {
                    Span span = column[j];
                    if (!span.walkable) continue;

                    for (int direction = 0; direction < 4; direction++)
                    {
                        int neighborX = span.x + NeighborX[direction];
                        int neighborZ = span.z + NeighborZ[direction];
                        if (neighborX < 0 || neighborX >= width || neighborZ < 0 || neighborZ >= depth)
                            continue;

                        List<Span> candidates = columns[neighborZ * width + neighborX];
                        span.neighbors[direction] = FindClosestWalkable(candidates, span.height, maxStepHeight);
                    }
                }
            }

            // 多层高度场中最近关系可能不是双向的。仅保留双方互选的边，避免生成单向普通邻接。
            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                {
                    Span span = column[j];
                    if (!span.walkable) continue;
                    for (int direction = 0; direction < 4; direction++)
                    {
                        Span neighbor = span.neighbors[direction];
                        if (neighbor != null && neighbor.neighbors[direction ^ 1] != span)
                            span.neighbors[direction] = null;
                    }
                }
            }
        }

        private static Span FindClosestWalkable(List<Span> column, LFloat height, LFloat maxStepHeight)
        {
            if (column == null) return null;

            Span best = null;
            LFloat bestDifference = LFloat.MaxValue;
            for (int i = 0; i < column.Count; i++)
            {
                Span candidate = column[i];
                if (!candidate.walkable) continue;

                LFloat difference = LMath.Abs(candidate.height - height);
                if (difference <= maxStepHeight && difference < bestDifference)
                {
                    best = candidate;
                    bestDifference = difference;
                }
            }
            return best;
        }

        private static void Erode(List<Span>[] columns, int width, int depth, int radius)
        {
            var distance = new Dictionary<Span, int>();
            var queue = new Queue<Span>();

            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                {
                    Span span = column[j];
                    if (!span.walkable) continue;

                    bool boundary = span.x == 0 || span.x == width - 1 || span.z == 0 || span.z == depth - 1;
                    if (!boundary)
                    {
                        for (int direction = 0; direction < 4; direction++)
                        {
                            if (span.neighbors[direction] == null)
                            {
                                boundary = true;
                                break;
                            }
                        }
                    }

                    if (boundary)
                    {
                        distance.Add(span, 0);
                        queue.Enqueue(span);
                    }
                }
            }

            while (queue.Count > 0)
            {
                Span span = queue.Dequeue();
                int current = distance[span];
                if (current >= radius) continue;

                for (int direction = 0; direction < 4; direction++)
                {
                    Span neighbor = span.neighbors[direction];
                    if (neighbor == null || distance.ContainsKey(neighbor)) continue;
                    distance.Add(neighbor, current + 1);
                    queue.Enqueue(neighbor);
                }
            }

            foreach (KeyValuePair<Span, int> pair in distance)
            {
                if (pair.Value < radius)
                    pair.Key.walkable = false;
            }
        }

        private static void RemoveSmallRegions(List<Span>[] columns, int minRegionCells)
        {
            if (minRegionCells <= 1) return;

            int nextRegion = 1;
            var queue = new Queue<Span>();
            var members = new List<Span>();
            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                {
                    Span seed = column[j];
                    if (!seed.walkable || seed.region != 0) continue;

                    members.Clear();
                    seed.region = nextRegion;
                    queue.Enqueue(seed);
                    while (queue.Count > 0)
                    {
                        Span span = queue.Dequeue();
                        members.Add(span);
                        for (int direction = 0; direction < 4; direction++)
                        {
                            Span neighbor = span.neighbors[direction];
                            if (neighbor == null || !neighbor.walkable || neighbor.region != 0) continue;
                            neighbor.region = nextRegion;
                            queue.Enqueue(neighbor);
                        }
                    }

                    if (members.Count < minRegionCells)
                    {
                        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
                            members[memberIndex].walkable = false;
                    }
                    nextRegion++;
                }
            }
        }

        private static List<Span> CollectWalkableSpans(List<Span>[] columns)
        {
            var result = new List<Span>();
            for (int i = 0; i < columns.Length; i++)
            {
                List<Span> column = columns[i];
                if (column == null) continue;
                for (int j = 0; j < column.Count; j++)
                {
                    if (column[j].walkable)
                        result.Add(column[j]);
                }
            }
            return result;
        }

        private static int CountSpans(List<Span>[] columns)
        {
            int count = 0;
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] != null)
                    count += columns[i].Count;
            }
            return count;
        }

        private static void Triangulate(
            NavData nav,
            List<Span> spans,
            NavBuildSettings settings,
            LFloat originX,
            LFloat originZ,
            ref NavBuildReport report)
        {
            LFloat cellSize = settings.cellSize;
            var vertexIndices = new Dictionary<VertexKey, int>();
            var vertices = new List<LVector3>();
            var edgeOwners = new Dictionary<EdgeKey, EdgeOwner>();
            var cells = new List<CellGeometry>(spans.Count);
            var cellsBySpan = new Dictionary<Span, CellGeometry>(spans.Count);

            for (int i = 0; i < spans.Count; i++)
            {
                Span span = spans[i];
                LFloat x0 = originX + cellSize * span.x;
                LFloat x1 = x0 + cellSize;
                LFloat z0 = originZ + cellSize * span.z;
                LFloat z1 = z0 + cellSize;

                var cell = new CellGeometry
                {
                    span = span,
                    p00 = new LVector3(x0, GetCornerHeight(span, 0, 2), z0),
                    p10 = new LVector3(x1, GetCornerHeight(span, 1, 2), z0),
                    p11 = new LVector3(x1, GetCornerHeight(span, 1, 3), z1),
                    p01 = new LVector3(x0, GetCornerHeight(span, 0, 3), z1)
                };
                cells.Add(cell);
                cellsBySpan.Add(span, cell);
            }

            List<DelaunayPatch> delaunayPatches =
                settings.mergeCoplanarCells && settings.useConstrainedDelaunay
                    ? BuildDelaunayPatches(cells, cellsBySpan, ref report)
                    : new List<DelaunayPatch>();
            List<RectanglePatch> rectangles = settings.mergeCoplanarCells
                ? MergeCoplanarCells(cells, cellsBySpan)
                : CreateSingleCellRectangles(cells);

            for (int i = 0; i < delaunayPatches.Count; i++)
            {
                DelaunayPatch patch = delaunayPatches[i];
                for (int triangleIndex = 0; triangleIndex < patch.triangles.Count; triangleIndex++)
                {
                    ConstrainedDelaunay.IndexTriangle triangle = patch.triangles[triangleIndex];
                    patch.outputTriangles.Add(AddTriangle(
                        nav,
                        patch.vertices[triangle.a],
                        patch.vertices[triangle.b],
                        patch.vertices[triangle.c],
                        vertices,
                        vertexIndices,
                        edgeOwners));
                }
                MapDelaunayBoundaryTriangles(nav, patch);
            }

            for (int i = 0; i < rectangles.Count; i++)
            {
                RectanglePatch rectangle = rectangles[i];
                rectangle.northEastTriangle = AddTriangle(
                    nav, rectangle.p00, rectangle.p11, rectangle.p10,
                    vertices, vertexIndices, edgeOwners);
                rectangle.southWestTriangle = AddTriangle(
                    nav, rectangle.p00, rectangle.p01, rectangle.p11,
                    vertices, vertexIndices, edgeOwners);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                CellGeometry cell = cells[i];
                if (cell.patch is RectanglePatch rectangle)
                {
                    for (int direction = 0; direction < 4; direction++)
                        cell.boundaryTriangles[direction] =
                            GetRectangleBoundaryTriangle(rectangle, direction);
                }
            }

            // 矩形简化会产生 T 形边界，不能只依赖“两个三角形拥有完全相同的边”建立邻接。
            // 这里沿用简化前高度场的四邻接关系，把每段真实可通过边映射到两侧矩形三角形。
            for (int i = 0; i < cells.Count; i++)
            {
                CellGeometry cell = cells[i];
                for (int direction = 0; direction < 4; direction++)
                {
                    Span neighborSpan = cell.span.neighbors[direction];
                    if (neighborSpan == null ||
                        !cellsBySpan.TryGetValue(neighborSpan, out CellGeometry neighbor) ||
                        ReferenceEquals(cell.patch, neighbor.patch))
                        continue;

                    int triangle = cell.boundaryTriangles[direction];
                    int neighborTriangle = neighbor.boundaryTriangles[direction ^ 1];
                    if (triangle < 0 || neighborTriangle < 0)
                        continue;
                    AddNeighbor(nav.triangles[triangle].neighbors, neighborTriangle);
                    AddNeighbor(nav.triangles[neighborTriangle].neighbors, triangle);
                }
            }
            report.mergedRectangles = rectangles.Count;
        }

        /// <summary>
        /// 找出可安全抽取轮廓的共面四连通区域，并尝试约束德洛内剖分。
        /// 成功区域会先标为 assigned，随后矩形合并只处理剩余的异面过渡带与回退区域。
        /// </summary>
        private static List<DelaunayPatch> BuildDelaunayPatches(
            List<CellGeometry> cells,
            Dictionary<Span, CellGeometry> cellsBySpan,
            ref NavBuildReport report)
        {
            var result = new List<DelaunayPatch>();
            var processed = new HashSet<CellGeometry>();
            var componentSet = new HashSet<CellGeometry>();
            var queue = new Queue<CellGeometry>();
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                CellGeometry seed = cells[cellIndex];
                if (processed.Contains(seed) || !IsDelaunayCandidate(seed, cellsBySpan))
                    continue;

                LVector3 planePoint = seed.span.sourcePlanePoint;
                LVector3 planeNormal = seed.span.sourcePlaneNormal;
                LFloat normalLength = planeNormal.magnitude;
                componentSet.Clear();
                queue.Clear();
                componentSet.Add(seed);
                queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    CellGeometry current = queue.Dequeue();
                    for (int direction = 0; direction < 4; direction++)
                    {
                        Span neighborSpan = current.span.neighbors[direction];
                        if (neighborSpan == null ||
                            !cellsBySpan.TryGetValue(neighborSpan, out CellGeometry neighbor) ||
                            componentSet.Contains(neighbor) ||
                            !IsDelaunayCandidate(neighbor, cellsBySpan) ||
                            !IsSpanOnPlane(neighbor.span, planePoint, planeNormal, normalLength))
                            continue;

                        componentSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }

                foreach (CellGeometry componentCell in componentSet)
                    processed.Add(componentCell);
                if (componentSet.Count < 2)
                    continue;

                if (!TryBuildDelaunayPatch(
                        componentSet,
                        planePoint,
                        planeNormal,
                        out DelaunayPatch patch))
                {
                    report.delaunayFallbackRegions++;
                    continue;
                }

                foreach (CellGeometry componentCell in componentSet)
                {
                    componentCell.assigned = true;
                    componentCell.patch = patch;
                    patch.cells.Add(componentCell);
                    patch.spans.Add(componentCell.span);
                }
                result.Add(patch);
                report.delaunayRegions++;
            }
            return result;
        }

        /// <summary>
        /// 判断单元能否进入轮廓剖分。相邻单元可以属于不同平面，但两平面必须在共享栅格边的
        /// 两个端点处完全接合；因此连续斜坡/平台可分别整体简化，有高度落差的台阶仍会回退。
        /// </summary>
        private static bool IsDelaunayCandidate(
            CellGeometry cell,
            Dictionary<Span, CellGeometry> cellsBySpan)
        {
            Span span = cell.span;
            LFloat normalLength = span.sourcePlaneNormal.magnitude;
            if (normalLength <= LFloat.EPSILON) return false;

            for (int direction = 0; direction < 4; direction++)
            {
                Span neighborSpan = span.neighbors[direction];
                if (neighborSpan == null) continue;
                if (IsSpanOnPlane(
                        neighborSpan,
                        span.sourcePlanePoint,
                        span.sourcePlaneNormal,
                        normalLength))
                    continue;
                if (!cellsBySpan.TryGetValue(neighborSpan, out CellGeometry neighbor) ||
                    !ArePlanesContinuousAcrossBoundary(cell, neighbor, direction))
                    return false;
            }
            return true;
        }

        private static bool ArePlanesContinuousAcrossBoundary(
            CellGeometry cell,
            CellGeometry neighbor,
            int direction)
        {
            GetCellBoundaryEdge(cell, direction, out LVector3 start, out LVector3 end);
            LVector3 currentStart = ProjectToPlane(
                start, cell.span.sourcePlanePoint, cell.span.sourcePlaneNormal);
            LVector3 currentEnd = ProjectToPlane(
                end, cell.span.sourcePlanePoint, cell.span.sourcePlaneNormal);
            LVector3 neighborStart = ProjectToPlane(
                start, neighbor.span.sourcePlanePoint, neighbor.span.sourcePlaneNormal);
            LVector3 neighborEnd = ProjectToPlane(
                end, neighbor.span.sourcePlanePoint, neighbor.span.sourcePlaneNormal);
            return NavHelper.SamePoint(currentStart, neighborStart) &&
                   NavHelper.SamePoint(currentEnd, neighborEnd);
        }

        private static bool TryBuildDelaunayPatch(
            HashSet<CellGeometry> component,
            LVector3 planePoint,
            LVector3 planeNormal,
            out DelaunayPatch patch)
        {
            patch = null;
            if (!TryExtractBoundaryLoops(
                    component,
                    planePoint,
                    planeNormal,
                    out List<List<LVector3>> loops))
                return false;

            if (!ConstrainedDelaunay.TryTriangulate(
                    loops,
                    out List<LVector3> vertices,
                    out List<ConstrainedDelaunay.IndexTriangle> triangles))
                return false;

            patch = new DelaunayPatch
            {
                vertices = vertices,
                triangles = triangles,
                planePoint = planePoint,
                planeNormal = planeNormal
            };
            return true;
        }

        private static bool TryExtractBoundaryLoops(
            HashSet<CellGeometry> component,
            LVector3 planePoint,
            LVector3 planeNormal,
            out List<List<LVector3>> loops)
        {
            loops = new List<List<LVector3>>();
            var edgesByStart = new Dictionary<VertexKey, LVector3>();
            var incomingCounts = new Dictionary<VertexKey, int>();
            var componentSpans = new HashSet<Span>();
            var pointsByKey = new Dictionary<VertexKey, LVector3>();
            foreach (CellGeometry cell in component)
                componentSpans.Add(cell.span);

            foreach (CellGeometry cell in component)
            {
                for (int direction = 0; direction < 4; direction++)
                {
                    Span neighbor = cell.span.neighbors[direction];
                    if (neighbor != null && componentSpans.Contains(neighbor))
                        continue;

                    GetCellBoundaryEdge(cell, direction, out LVector3 start, out LVector3 end);
                    start = ProjectToPlane(start, planePoint, planeNormal);
                    end = ProjectToPlane(end, planePoint, planeNormal);
                    var startKey = new VertexKey(start);
                    var endKey = new VertexKey(end);
                    // 四连通区域在对角接触或自接触顶点处可能出现多个出边；这种弱轮廓交给安全回退。
                    if (edgesByStart.ContainsKey(startKey)) return false;
                    edgesByStart.Add(startKey, end);
                    pointsByKey[startKey] = start;
                    pointsByKey[endKey] = end;
                    incomingCounts.TryGetValue(endKey, out int incoming);
                    incomingCounts[endKey] = incoming + 1;
                }
            }

            foreach (KeyValuePair<VertexKey, LVector3> edge in edgesByStart)
            {
                if (!incomingCounts.TryGetValue(edge.Key, out int incoming) || incoming != 1)
                    return false;
            }

            var unused = new HashSet<VertexKey>(edgesByStart.Keys);
            while (unused.Count > 0)
            {
                VertexKey firstKey = default;
                bool hasFirst = false;
                foreach (VertexKey key in unused)
                {
                    if (!hasFirst || key.CompareTo(firstKey) < 0)
                    {
                        firstKey = key;
                        hasFirst = true;
                    }
                }
                if (!hasFirst) break;

                var loop = new List<LVector3>();
                VertexKey currentKey = firstKey;
                int guard = edgesByStart.Count + 1;
                do
                {
                    if (!unused.Remove(currentKey) ||
                        !edgesByStart.TryGetValue(currentKey, out LVector3 end))
                        return false;
                    if (!pointsByKey.TryGetValue(currentKey, out LVector3 start))
                        return false;
                    loop.Add(start);
                    currentKey = new VertexKey(end);
                    guard--;
                    if (guard <= 0) return false;
                } while (!currentKey.Equals(firstKey));
                if (loop.Count < 3) return false;
                loops.Add(loop);
            }

            int outerIndex = -1;
            for (int i = 0; i < loops.Count; i++)
            {
                BigInteger area = GetSignedAreaXZ(loops[i]);
                if (area > BigInteger.Zero)
                {
                    if (outerIndex >= 0) return false;
                    outerIndex = i;
                }
            }
            if (outerIndex < 0) return false;
            if (outerIndex != 0)
            {
                List<LVector3> outer = loops[outerIndex];
                loops[outerIndex] = loops[0];
                loops[0] = outer;
            }
            return true;
        }

        private static BigInteger GetSignedAreaXZ(List<LVector3> loop)
        {
            BigInteger area = BigInteger.Zero;
            for (int i = 0; i < loop.Count; i++)
            {
                LVector3 current = loop[i];
                LVector3 next = loop[(i + 1) % loop.Count];
                area +=
                    (BigInteger)current._x * next._z -
                    (BigInteger)next._x * current._z;
            }
            return area;
        }

        private static void GetCellBoundaryEdge(
            CellGeometry cell,
            int direction,
            out LVector3 start,
            out LVector3 end)
        {
            switch (direction)
            {
                case 0:
                    start = cell.p01;
                    end = cell.p00;
                    return;
                case 1:
                    start = cell.p10;
                    end = cell.p11;
                    return;
                case 2:
                    start = cell.p00;
                    end = cell.p10;
                    return;
                default:
                    start = cell.p11;
                    end = cell.p01;
                    return;
            }
        }

        private static void MapDelaunayBoundaryTriangles(NavData nav, DelaunayPatch patch)
        {
            var verticalEdges = new Dictionary<long, List<BoundaryEdgeOwner>>();
            var horizontalEdges = new Dictionary<long, List<BoundaryEdgeOwner>>();
            for (int i = 0; i < patch.outputTriangles.Count; i++)
            {
                int triangleIndex = patch.outputTriangles[i];
                Triangle triangle = nav.triangles[triangleIndex];
                for (int edgeIndex = 0; edgeIndex < triangle.edges.Length; edgeIndex++)
                {
                    Edge edge = triangle.edges[edgeIndex];
                    Dictionary<long, List<BoundaryEdgeOwner>> index;
                    long line;
                    if (edge.a._x == edge.b._x)
                    {
                        index = verticalEdges;
                        line = edge.a._x;
                    }
                    else if (edge.a._z == edge.b._z)
                    {
                        index = horizontalEdges;
                        line = edge.a._z;
                    }
                    else
                    {
                        continue;
                    }

                    if (!index.TryGetValue(line, out List<BoundaryEdgeOwner> owners))
                    {
                        owners = new List<BoundaryEdgeOwner>();
                        index.Add(line, owners);
                    }
                    owners.Add(new BoundaryEdgeOwner(edge, triangleIndex));
                }
            }

            for (int cellIndex = 0; cellIndex < patch.cells.Count; cellIndex++)
            {
                CellGeometry cell = patch.cells[cellIndex];
                for (int direction = 0; direction < 4; direction++)
                {
                    Span neighbor = cell.span.neighbors[direction];
                    if (neighbor != null && patch.spans.Contains(neighbor))
                        continue;

                    GetCellBoundaryEdge(cell, direction, out LVector3 edgeStart, out LVector3 edgeEnd);
                    edgeStart = ProjectToPlane(edgeStart, patch.planePoint, patch.planeNormal);
                    edgeEnd = ProjectToPlane(edgeEnd, patch.planePoint, patch.planeNormal);
                    Edge cellEdge = Edge.Create(edgeStart, edgeEnd);
                    Dictionary<long, List<BoundaryEdgeOwner>> index = direction < 2
                        ? verticalEdges
                        : horizontalEdges;
                    long line = direction < 2 ? edgeStart._x : edgeStart._z;
                    if (!index.TryGetValue(line, out List<BoundaryEdgeOwner> candidates))
                        continue;

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        BoundaryEdgeOwner candidate = candidates[i];
                        if (!NavHelper.TryGetOverlappingEdge(
                                candidate.edge, cellEdge, out _))
                            continue;
                        cell.boundaryTriangles[direction] = candidate.triangle;
                        break;
                    }
                }
            }
        }

        private static List<RectanglePatch> MergeCoplanarCells(
            List<CellGeometry> cells,
            Dictionary<Span, CellGeometry> cellsBySpan)
        {
            var result = new List<RectanglePatch>();
            var rows = new List<List<CellGeometry>>();
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                CellGeometry seed = cells[cellIndex];
                if (seed.assigned) continue;

                LVector3 planeNormal = seed.span.sourcePlaneNormal;
                LFloat normalLength = planeNormal.magnitude;
                bool planarSeed =
                    normalLength > LFloat.EPSILON &&
                    HasOnlyCoplanarNeighbors(
                        seed.span,
                        seed.span.sourcePlanePoint,
                        planeNormal,
                        normalLength);
                int bestWidth = 1;
                int bestDepth = 1;
                int bestArea = 1;
                rows.Clear();

                if (planarSeed)
                {
                    List<CellGeometry> firstRow = CollectCompatibleRow(
                        seed,
                        null,
                        int.MaxValue,
                        seed.span.sourcePlanePoint,
                        planeNormal,
                        normalLength,
                        cellsBySpan);
                    rows.Add(firstRow);
                    int currentWidth = firstRow.Count;
                    bestWidth = currentWidth;
                    bestArea = currentWidth;

                    List<CellGeometry> previousRow = firstRow;
                    while (currentWidth > 0)
                    {
                        Span nextSeedSpan = previousRow[0].span.neighbors[3];
                        if (nextSeedSpan == null ||
                            !cellsBySpan.TryGetValue(nextSeedSpan, out CellGeometry nextSeed))
                            break;

                        List<CellGeometry> nextRow = CollectCompatibleRow(
                            nextSeed,
                            previousRow,
                            currentWidth,
                            seed.span.sourcePlanePoint,
                            planeNormal,
                            normalLength,
                            cellsBySpan);
                        if (nextRow.Count == 0) break;

                        rows.Add(nextRow);
                        currentWidth = LMath.Min(currentWidth, nextRow.Count);
                        int area = currentWidth * rows.Count;
                        if (area > bestArea)
                        {
                            bestArea = area;
                            bestWidth = currentWidth;
                            bestDepth = rows.Count;
                        }
                        previousRow = nextRow;
                    }
                }

                var rectangle = new RectanglePatch();
                CellGeometry bottomLeft = seed;
                for (int z = 0; z < bestDepth; z++)
                {
                    List<CellGeometry> row;
                    if (z < rows.Count)
                    {
                        row = rows[z];
                    }
                    else
                    {
                        row = new List<CellGeometry> { bottomLeft };
                    }

                    for (int x = 0; x < bestWidth; x++)
                    {
                        CellGeometry cell = row[x];
                        cell.assigned = true;
                        cell.patch = rectangle;
                    }
                    bottomLeft = row[0];
                }

                CellGeometry topLeft = rows.Count > 0 ? rows[0][0] : seed;
                CellGeometry topRight = rows.Count > 0 ? rows[0][bestWidth - 1] : seed;
                CellGeometry lowerLeft = rows.Count >= bestDepth ? rows[bestDepth - 1][0] : seed;
                CellGeometry lowerRight = rows.Count >= bestDepth ? rows[bestDepth - 1][bestWidth - 1] : seed;
                if (planarSeed)
                {
                    // 高度场存的是单元中心高度，边界角点因缺少外侧样本会有半格偏差。
                    // 合并后按原始三角面重新求四角高度，既恢复正确坡面，也避免把采样误差
                    // 固化为一张略微扭曲的大三角形。
                    rectangle.p00 = ProjectToPlane(
                        topLeft.p00, seed.span.sourcePlanePoint, planeNormal);
                    rectangle.p10 = ProjectToPlane(
                        topRight.p10, seed.span.sourcePlanePoint, planeNormal);
                    rectangle.p11 = ProjectToPlane(
                        lowerRight.p11, seed.span.sourcePlanePoint, planeNormal);
                    rectangle.p01 = ProjectToPlane(
                        lowerLeft.p01, seed.span.sourcePlanePoint, planeNormal);
                }
                else
                {
                    // 异面接缝单元保留原来的角点平均结果，让台阶两侧仍拥有完全相同的 Portal。
                    rectangle.p00 = topLeft.p00;
                    rectangle.p10 = topRight.p10;
                    rectangle.p11 = lowerRight.p11;
                    rectangle.p01 = lowerLeft.p01;
                }
                result.Add(rectangle);
            }
            return result;
        }

        /// <summary>
        /// 只有四邻接都属于同一平面的单元才进入大矩形。
        /// 靠近坡面折线或可跨越台阶的单元保持逐格三角化，作为一格宽的过渡带；这样大矩形
        /// 不会用一条直边跨过多个不同高度的 Portal，同时平面内部仍可压缩到很少的三角形。
        /// </summary>
        private static bool HasOnlyCoplanarNeighbors(
            Span span,
            LVector3 planePoint,
            LVector3 planeNormal,
            LFloat normalLength)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                Span neighbor = span.neighbors[direction];
                if (neighbor != null &&
                    !IsSpanOnPlane(neighbor, planePoint, planeNormal, normalLength))
                    return false;
            }
            return true;
        }

        private static LVector3 ProjectToPlane(
            LVector3 point,
            LVector3 planePoint,
            LVector3 planeNormal)
        {
            LFloat y = planePoint.y -
                (planeNormal.x * (point.x - planePoint.x) +
                 planeNormal.z * (point.z - planePoint.z)) /
                planeNormal.y;
            return new LVector3(point.x, y, point.z);
        }

        private static List<CellGeometry> CollectCompatibleRow(
            CellGeometry start,
            List<CellGeometry> previousRow,
            int maxWidth,
            LVector3 planePoint,
            LVector3 planeNormal,
            LFloat normalLength,
            Dictionary<Span, CellGeometry> cellsBySpan)
        {
            var row = new List<CellGeometry>();
            CellGeometry current = start;
            while (current != null && row.Count < maxWidth)
            {
                if (current.assigned ||
                    !IsSpanOnPlane(current.span, planePoint, planeNormal, normalLength) ||
                    !HasOnlyCoplanarNeighbors(
                        current.span, planePoint, planeNormal, normalLength))
                    break;

                int x = row.Count;
                if (previousRow != null &&
                    (x >= previousRow.Count || previousRow[x].span.neighbors[3] != current.span))
                    break;
                if (x > 0 && row[x - 1].span.neighbors[1] != current.span)
                    break;

                row.Add(current);
                Span next = current.span.neighbors[1];
                current = next != null && cellsBySpan.TryGetValue(next, out CellGeometry value)
                    ? value
                    : null;
            }
            return row;
        }

        private static bool IsSpanOnPlane(
            Span span,
            LVector3 planePoint,
            LVector3 planeNormal,
            LFloat normalLength)
        {
            LFloat tolerance = NavHelper.epsilon * normalLength;
            LFloat sourceNormalLength = span.sourcePlaneNormal.magnitude;
            if (sourceNormalLength <= LFloat.EPSILON) return false;

            LFloat parallelTolerance = NavHelper.epsilon * normalLength * sourceNormalLength;
            return LVector3.Cross(planeNormal, span.sourcePlaneNormal).magnitude <= parallelTolerance &&
                   LMath.Abs(LVector3.Dot(planeNormal, span.samplePoint - planePoint)) <= tolerance;
        }

        private static List<RectanglePatch> CreateSingleCellRectangles(List<CellGeometry> cells)
        {
            var result = new List<RectanglePatch>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                CellGeometry cell = cells[i];
                var rectangle = new RectanglePatch
                {
                    p00 = cell.p00,
                    p10 = cell.p10,
                    p11 = cell.p11,
                    p01 = cell.p01
                };
                cell.assigned = true;
                cell.patch = rectangle;
                result.Add(rectangle);
            }
            return result;
        }

        private static int GetRectangleBoundaryTriangle(RectanglePatch rectangle, int direction)
        {
            // 固定对角线 p00-p11 后，北/东边属于第一个三角形，南/西边属于第二个。
            return direction == 1 || direction == 2
                ? rectangle.northEastTriangle
                : rectangle.southWestTriangle;
        }

        private static LFloat GetCornerHeight(Span span, int xDirection, int zDirection)
        {
            // 一个栅格角点最多由当前单元、两个正交邻居以及经任一邻居到达的对角单元共享。
            // T/L 形边界上并不要求四格齐全；只要单元通过该角附近的普通邻接相连，就必须采用
            // 同一组高度平均，否则相邻单元会生成不同的边端点，最终在导航三角形之间留下裂缝。
            Span xNeighbor = span.neighbors[xDirection];
            Span zNeighbor = span.neighbors[zDirection];
            Span diagonalFromX = xNeighbor?.neighbors[zDirection];
            Span diagonalFromZ = zNeighbor?.neighbors[xDirection];

            LFloat sum = span.height;
            int count = 1;
            if (xNeighbor != null)
            {
                sum += xNeighbor.height;
                count++;
            }
            if (zNeighbor != null && !ReferenceEquals(zNeighbor, xNeighbor))
            {
                sum += zNeighbor.height;
                count++;
            }
            if (diagonalFromX != null &&
                !ReferenceEquals(diagonalFromX, span) &&
                !ReferenceEquals(diagonalFromX, xNeighbor) &&
                !ReferenceEquals(diagonalFromX, zNeighbor))
            {
                sum += diagonalFromX.height;
                count++;
            }
            if (diagonalFromZ != null &&
                !ReferenceEquals(diagonalFromZ, span) &&
                !ReferenceEquals(diagonalFromZ, xNeighbor) &&
                !ReferenceEquals(diagonalFromZ, zNeighbor) &&
                !ReferenceEquals(diagonalFromZ, diagonalFromX))
            {
                sum += diagonalFromZ.height;
                count++;
            }
            return sum / count;
        }

        private static int AddTriangle(
            NavData nav,
            LVector3 a,
            LVector3 b,
            LVector3 c,
            List<LVector3> vertices,
            Dictionary<VertexKey, int> vertexIndices,
            Dictionary<EdgeKey, EdgeOwner> edgeOwners)
        {
            int aIndex = GetVertexIndex(a, vertices, vertexIndices);
            int bIndex = GetVertexIndex(b, vertices, vertexIndices);
            int cIndex = GetVertexIndex(c, vertices, vertexIndices);
            Triangle triangle = CreateTriangle(a, b, c);
            int triangleIndex = nav.triangles.Count;
            nav.triangles.Add(triangle);

            AddEdgeOwner(nav, new EdgeKey(aIndex, bIndex), triangleIndex, edgeOwners);
            AddEdgeOwner(nav, new EdgeKey(bIndex, cIndex), triangleIndex, edgeOwners);
            AddEdgeOwner(nav, new EdgeKey(cIndex, aIndex), triangleIndex, edgeOwners);
            return triangleIndex;
        }

        private static int GetVertexIndex(
            LVector3 vertex,
            List<LVector3> vertices,
            Dictionary<VertexKey, int> vertexIndices)
        {
            var key = new VertexKey(vertex);
            if (vertexIndices.TryGetValue(key, out int index)) return index;

            index = vertices.Count;
            vertices.Add(vertex);
            vertexIndices.Add(key, index);
            return index;
        }

        private static void AddEdgeOwner(
            NavData nav,
            EdgeKey edge,
            int triangle,
            Dictionary<EdgeKey, EdgeOwner> edgeOwners)
        {
            if (!edgeOwners.TryGetValue(edge, out EdgeOwner owner))
            {
                edgeOwners.Add(edge, new EdgeOwner(triangle));
                return;
            }

            if (owner.triangle == triangle) return;
            AddNeighbor(nav.triangles[owner.triangle].neighbors, triangle);
            AddNeighbor(nav.triangles[triangle].neighbors, owner.triangle);
        }

        private static void AddNeighbor(List<int> neighbors, int value)
        {
            if (!neighbors.Contains(value))
                neighbors.Add(value);
        }

        private static Triangle CreateTriangle(LVector3 a, LVector3 b, LVector3 c)
        {
            var triangle = new Triangle();
            triangle.points[0] = a;
            triangle.points[1] = b;
            triangle.points[2] = c;
            triangle.edges[0] = Edge.Create(a, b);
            triangle.edges[1] = Edge.Create(b, c);
            triangle.edges[2] = Edge.Create(c, a);
            triangle.bounds.SetMinMax(
                new LVector3(
                    LMath.Min(a.x, b.x, c.x),
                    LMath.Min(a.y, b.y, c.y),
                    LMath.Min(a.z, b.z, c.z)),
                new LVector3(
                    LMath.Max(a.x, b.x, c.x),
                    LMath.Max(a.y, b.y, c.y),
                    LMath.Max(a.z, b.z, c.z)));
            return triangle;
        }

        private static void AddLinks(NavData nav, IList<NavBuildLink> links, ref NavBuildReport report)
        {
            if (links == null || links.Count == 0 || nav.triangles.Count == 0) return;

            var triangleIndices = new Dictionary<Triangle, int>(nav.triangles.Count);
            for (int i = 0; i < nav.triangles.Count; i++)
                triangleIndices.Add(nav.triangles[i], i);

            var locator = new NavMap(nav);
            for (int i = 0; i < links.Count; i++)
            {
                NavBuildLink link = links[i];
                LVector3 from = link.from;
                LVector3 to = link.to;
                if (!locator.TryGetTriangle(from, out Triangle fromTriangle, out from) ||
                    !locator.TryGetTriangle(to, out Triangle toTriangle, out to) ||
                    ReferenceEquals(fromTriangle, toTriangle))
                {
                    report.rejectedLinks++;
                    continue;
                }

                // 离线链接允许零代价。这里不擅自按端点距离补值，确保生成结果严格采用业务层配置，
                // 同时兼容旧导出逻辑中 costModifier=-1 被截断为零的行为。
                LFloat cost = LMath.Max(link.cost, LFloat.zero);
                fromTriangle.links.Add(new TriangleLink
                {
                    from = from,
                    to = to,
                    cost = cost,
                    neighbor = triangleIndices[toTriangle]
                });
                report.addedLinks++;

                if (link.bidirectional)
                {
                    toTriangle.links.Add(new TriangleLink
                    {
                        from = to,
                        to = from,
                        cost = cost,
                        neighbor = triangleIndices[fromTriangle]
                    });
                    report.addedLinks++;
                }
            }
        }
    }
}
