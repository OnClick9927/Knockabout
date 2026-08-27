using System;
using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// 把 RVO 的 XZ 平面位移限制在连续的普通导航三角形上。
    /// <para>
    /// 该类只由 NavRvoWorld 使用。它不会修改 NavMap 或 Triangle，而是根据 NavData 中已有的
    /// neighbors 穿越共享边。TriangleLink 属于离散跳转，必须由 NavRvoAgent 的 Link 流程处理。
    /// </para>
    /// </summary>
    internal sealed class NavMeshConstraint
    {
        private readonly NavMap map;
        private readonly List<Triangle> triangles;
        private readonly Dictionary<Triangle, int> triangleIndices;
        private readonly int[] fanVisitVersions;
        private readonly List<int> fanQueue = new List<int>();
        private int fanVisitVersion;

        public NavMeshConstraint(NavMap map, NavData data)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            if (data == null) throw new ArgumentNullException(nameof(data));

            triangles = data.triangles == null
                ? new List<Triangle>()
                : new List<Triangle>(data.triangles);
            triangleIndices = new Dictionary<Triangle, int>(triangles.Count);
            fanVisitVersions = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle triangle = triangles[i];
                if (triangle == null)
                    throw new ArgumentException("NavData contains a null triangle.", nameof(data));
                if (!triangleIndices.ContainsKey(triangle))
                    triangleIndices.Add(triangle, i);
            }
        }

        /// <summary>验证 XZ 投影属于 NavMesh，并把 Y 吸附到高度最近的三角形平面。</summary>
        public bool TryPlace(
            LVector3 point,
            out Triangle triangle,
            out LVector3 snappedPoint)
        {
            return map.TryGetTriangle(point, out triangle, out snappedPoint);
        }

        /// <summary>
        /// 沿 previous 到 candidate 的线段遍历普通相邻三角形。
        /// 成功到达候选点时 constrained 为 false；碰到没有普通邻居的边界时返回边界交点，
        /// constrained 为 true。无论哪种成功结果，返回位置都位于 returnedTriangle 的平面上。
        /// </summary>
        public bool TryConstrainMove(
            Triangle currentTriangle,
            LVector3 previous,
            LVector3 candidate,
            out Triangle returnedTriangle,
            out LVector3 returnedPosition,
            out bool constrained)
        {
            returnedTriangle = currentTriangle;
            returnedPosition = previous;
            constrained = false;
            if (currentTriangle == null || !triangleIndices.ContainsKey(currentTriangle))
                return false;

            Triangle triangle = currentTriangle;
            LVector3 segmentStart = triangle.Snap(previous);
            LVector3 planarTarget = new LVector3(candidate.x, segmentStart.y, candidate.z);
            if (SamePlanarPoint(segmentStart, planarTarget))
            {
                ResolveSurfaceTriangle(triangle, planarTarget, out returnedTriangle, out returnedPosition);
                return true;
            }

            // 一条线段穿越同一个三角形两次只可能发生在退化数据中；用三角形总数作为硬上限，
            // 防止错误邻接或顶点数值误差造成无限循环。
            int remainingTransitions = triangles.Count + 1;
            while (remainingTransitions-- > 0)
            {
                if (triangle.ContainsPointXZ(planarTarget))
                {
                    ResolveSurfaceTriangle(triangle, planarTarget, out returnedTriangle, out returnedPosition);
                    return true;
                }

                if (!TryFindExitPoint(triangle, segmentStart, planarTarget, out LVector3 exitPoint))
                {
                    // previous 理论上位于 currentTriangle 内。若运行时 NavData 被修改导致重心计算失败，
                    // 强制回到当前三角形内部比采用一个未经验证的 RVO 候选位置更安全。
                    returnedTriangle = triangle;
                    returnedPosition = ClampInsideTriangle(triangle, segmentStart);
                    constrained = true;
                    return true;
                }

                Triangle neighbor = FindNeighborAfterExit(triangle, exitPoint, planarTarget);
                if (neighbor == null)
                {
                    // 外边界采用朝三角形内部的极小回退，而不是停在同时属于多个闭区间的边上。
                    // 这既能抵消定点舍入，也能让后续自动重寻路稳定选择当前连通面。
                    ResolveSurfaceTriangle(
                        triangle,
                        CreateProbePoint(exitPoint, triangle.GetInteriorPoint()),
                        out returnedTriangle,
                        out returnedPosition);
                    constrained = true;
                    return true;
                }

                triangle = neighbor;
                // 从边界沿移动方向推进一个极小量，使下一次重心坐标明确位于新三角形内，
                // 避免两个三角形都把共享边视为闭区间时反复来回选择。
                segmentStart = CreateProbePoint(exitPoint, planarTarget);
                segmentStart = triangle.Snap(segmentStart);
            }

            returnedTriangle = triangle;
            returnedPosition = ClampInsideTriangle(triangle, segmentStart);
            constrained = true;
            return true;
        }

        /// <summary>
        /// 取得指定导航三角形内不会落在共享边或孔洞边界上的稳定恢复点。
        /// 只接受构造 NavMeshConstraint 时登记过的三角形，防止外部伪造几何绕过导航数据。
        /// </summary>
        public bool TryGetRecoveryPoint(Triangle triangle, out LVector3 recoveryPoint)
        {
            recoveryPoint = LVector3.zero;
            if (triangle == null || !triangleIndices.ContainsKey(triangle))
                return false;

            recoveryPoint = triangle.GetInteriorPoint();
            return triangle.ContainsPointXZ(recoveryPoint);
        }

        /// <summary>
        /// 在边穿越已经确认 XZ 可达后，重新确认终点真正所属的表面三角形并计算高度。
        /// <para>
        /// 射线恰好穿过多个三角形共用的顶点时，确定性探针可能先落入一个仅在该顶点相接的邻面；
        /// 平地中这些面的 Y 相同，看不出区别，但崎岖地面会让错误邻面的平面外推产生轻微悬空。
        /// 此处只校正已经通过逐边遍历的终点，不用全局查询替代穿越过程，所以不会跨过孔洞或外边界。
        /// </para>
        /// </summary>
        private void ResolveSurfaceTriangle(
            Triangle traversedTriangle,
            LVector3 planarTarget,
            out Triangle resolvedTriangle,
            out LVector3 resolvedPosition)
        {
            LVector3 traversedPosition = traversedTriangle.Snap(planarTarget);
            if (map.TryGetTriangle(
                    traversedPosition,
                    out Triangle surfaceTriangle,
                    out LVector3 surfacePosition))
            {
                resolvedTriangle = surfaceTriangle;
                resolvedPosition = surfacePosition;
                return;
            }

            // 定点交点在孔洞拐角附近可能比边界多走几个千分位。此时不能直接拿当前三角形平面
            // 外推 Y，因为 XZ 仍可能位于孔洞中；先把重心权重钳制回三角形，再轻微朝内部退让。
            resolvedTriangle = traversedTriangle;
            resolvedPosition = ClampInsideTriangle(traversedTriangle, planarTarget);
        }

        /// <summary>通过钳制 XZ 重心权重把任意点放回指定三角形内部，并重新计算曲面高度。</summary>
        private static LVector3 ClampInsideTriangle(Triangle triangle, LVector3 point)
        {
            if (!triangle.TryGetXZBarycentric(point, out LFloat u, out LFloat v, out LFloat w))
                return triangle.GetInteriorPoint();

            u = LMath.Max(u, LFloat.zero);
            v = LMath.Max(v, LFloat.zero);
            w = LMath.Max(w, LFloat.zero);
            LFloat weightSum = u + v + w;
            if (weightSum <= LFloat.EPSILON)
                return triangle.GetInteriorPoint();

            u /= weightSum;
            v /= weightSum;
            w /= weightSum;
            LVector3 boundaryPoint = new LVector3(
                triangle.point1.x * u + triangle.point2.x * v + triangle.point3.x * w,
                LFloat.zero,
                triangle.point1.z * u + triangle.point2.z * v + triangle.point3.z * w);
            LVector3 insidePoint = CreateProbePoint(boundaryPoint, triangle.GetInteriorPoint());
            return triangle.Snap(insidePoint);
        }

        /// <summary>
        /// 根据起点和终点的 XZ 重心坐标，求线段第一次离开当前三角形的位置。
        /// 某个顶点权重降到零，表示线段穿过该顶点对面的边。
        /// </summary>
        private static bool TryFindExitPoint(
            Triangle triangle,
            LVector3 start,
            LVector3 target,
            out LVector3 exitPoint)
        {
            exitPoint = start;
            if (!triangle.TryGetXZBarycentric(start, out LFloat startU, out LFloat startV, out LFloat startW) ||
                !triangle.TryGetXZBarycentric(target, out LFloat targetU, out LFloat targetV, out LFloat targetW))
                return false;

            LFloat exitRatio = LFloat.one;
            bool found = false;
            SelectExitRatio(startU, targetU, ref exitRatio, ref found);
            SelectExitRatio(startV, targetV, ref exitRatio, ref found);
            SelectExitRatio(startW, targetW, ref exitRatio, ref found);
            if (!found) return false;

            exitRatio = LMath.Clamp01(exitRatio);
            LVector3 delta = target - start;
            exitPoint = new LVector3(
                start.x + delta.x * exitRatio,
                start.y,
                start.z + delta.z * exitRatio);
            return true;
        }

        private static void SelectExitRatio(
            LFloat startWeight,
            LFloat targetWeight,
            ref LFloat exitRatio,
            ref bool found)
        {
            // ContainsPointXZ 容忍 epsilon 负误差，因此只有明确越过容差的权重才视为离开。
            if (targetWeight >= -NavHelper.epsilon) return;

            LFloat denominator = startWeight - targetWeight;
            if (denominator <= LFloat.EPSILON) return;
            LFloat ratio = startWeight / denominator;
            if (ratio < LFloat.zero || ratio > exitRatio) return;

            exitRatio = ratio;
            found = true;
        }

        /// <summary>
        /// 从普通邻接中选择真正位于穿越方向一侧的三角形。
        /// <para>
        /// 普通穿边只需检查当前三角形的一层邻居；射线精确经过共享顶点时，目标方向所在三角形
        /// 可能隔着顶点扇区中的多片三角形。此时只沿“同样包含出口顶点”的普通邻接做广度搜索，
        /// 既能绕过网格顶点完成转向，又不能跨越没有邻接关系的孔洞或独立导航岛。
        /// </para>
        /// </summary>
        private Triangle FindNeighborAfterExit(
            Triangle triangle,
            LVector3 exitPoint,
            LVector3 target)
        {
            if (!triangleIndices.TryGetValue(triangle, out int triangleIndex))
                return null;

            LVector3 probe = CreateProbePoint(exitPoint, target);
            BeginFanSearch();
            fanVisitVersions[triangleIndex] = fanVisitVersion;
            fanQueue.Add(triangleIndex);

            for (int queueIndex = 0; queueIndex < fanQueue.Count; queueIndex++)
            {
                int currentIndex = fanQueue[queueIndex];
                List<int> neighbors = triangles[currentIndex].neighbors;
                if (neighbors == null) continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighborIndex = neighbors[i];
                    if (neighborIndex < 0 ||
                        neighborIndex >= triangles.Count ||
                        fanVisitVersions[neighborIndex] == fanVisitVersion)
                        continue;

                    fanVisitVersions[neighborIndex] = fanVisitVersion;
                    Triangle neighbor = triangles[neighborIndex];
                    if (!neighbor.ContainsPointXZ(exitPoint))
                        continue;

                    if (neighbor.ContainsPointXZ(probe))
                        return neighbor;

                    fanQueue.Add(neighborIndex);
                }
            }

            return null;
        }

        /// <summary>复用访问数组和队列开始一次顶点扇区搜索，版本溢出时才整体清零。</summary>
        private void BeginFanSearch()
        {
            fanQueue.Clear();
            if (fanVisitVersion == int.MaxValue)
            {
                Array.Clear(fanVisitVersions, 0, fanVisitVersions.Length);
                fanVisitVersion = 1;
            }
            else
            {
                fanVisitVersion++;
            }
        }

        private static LVector3 CreateProbePoint(LVector3 from, LVector3 target)
        {
            LVector2 direction = new LVector2(target.x - from.x, target.z - from.z);
            LFloat length = direction.magnitude;
            if (length <= LFloat.EPSILON) return from;

            // 探针最多推进剩余距离的一半，短线段不会因为固定 epsilon 而越过最终目标。
            LFloat probeDistance = LMath.Min(
                length / 2,
                NavHelper.epsilon * LFloat.FromRaw(4000000L));
            LVector2 offset = direction / length * probeDistance;
            return new LVector3(from.x + offset.x, from.y, from.z + offset.y);
        }

        private static bool SamePlanarPoint(LVector3 a, LVector3 b)
        {
            return LMath.Abs(a.x - b.x) <= NavHelper.epsilon &&
                   LMath.Abs(a.z - b.z) <= NavHelper.epsilon;
        }
    }
}
