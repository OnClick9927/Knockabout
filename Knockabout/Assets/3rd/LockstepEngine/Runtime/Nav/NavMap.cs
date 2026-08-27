using System;
using System.Collections.Generic;
using static Lockstep.Nav.NavPathPoint;

namespace Lockstep.Nav
{
    /// <summary>
    /// 确定性三角导航图。
    /// 构造时建立三角形索引、共享边映射和 BVH；Search 先执行 A* 找到三角形走廊，
    /// 再把相邻三角形逐片展开到二维平面并运行漏斗算法，最后插入 Link 语义点。
    /// 搜索上下文通过线程安全栈复用，避免并发查询共享临时数组。
    /// </summary>
    public class NavMap
    {
        private readonly struct TrianglePathStep
        {
            public readonly int index;
            public readonly TriangleLink incomingLink;

            public TrianglePathStep(int index, TriangleLink incomingLink)
            {
                this.index = index;
                this.incomingLink = incomingLink;
            }
        }

        private readonly struct Transition
        {
            public readonly int neighbor;
            public readonly LFloat cost;
            public readonly TriangleLink link;

            public Transition(int neighbor, LFloat cost, TriangleLink link)
            {
                this.neighbor = neighbor;
                this.cost = cost;
                this.link = link;
            }
        }

        private readonly struct UnfoldedTriangle
        {
            public readonly Triangle triangle;
            public readonly LVector2 point1;
            public readonly LVector2 point2;
            public readonly LVector2 point3;
            public LVector2 center => new LVector2(
                (point1.x + point2.x + point3.x) / 3,
                (point1.y + point2.y + point3.y) / 3);

            public UnfoldedTriangle(
                Triangle triangle,
                LVector2 point1,
                LVector2 point2,
                LVector2 point3)
            {
                this.triangle = triangle;
                this.point1 = point1;
                this.point2 = point2;
                this.point3 = point3;
            }
        }

        private readonly struct Portal
        {
            public readonly LVector3 leftPosition;
            public readonly LVector3 rightPosition;
            public readonly LVector2 left;
            public readonly LVector2 right;

            public Portal(
                LVector3 leftPosition,
                LVector3 rightPosition,
                LVector2 left,
                LVector2 right)
            {
                this.leftPosition = leftPosition;
                this.rightPosition = rightPosition;
                this.left = left;
                this.right = right;
            }
        }

        /// <summary>
        /// 单次 A* 与平滑流程的全部可变工作内存，使用 generation 延迟清空节点数组。
        /// </summary>
        private sealed class SearchContext
        {
            public readonly LFloat[] gScore;
            public readonly LFloat[] fScore;
            public readonly int[] parent;
            public readonly TriangleLink[] parentLink;
            public readonly int[] nodeState;
            public readonly MinHeap heap = new MinHeap();
            public readonly List<TrianglePathStep> trianglePath = new List<TrianglePathStep>();
            public readonly List<Portal> portals = new List<Portal>();
            public readonly List<LVector3> funnelResult = new List<LVector3>();
            public int version;

            public SearchContext(int nodeCount)
            {
                gScore = new LFloat[nodeCount];
                fScore = new LFloat[nodeCount];
                parent = new int[nodeCount];
                parentLink = new TriangleLink[nodeCount];
                nodeState = new int[nodeCount];
            }

            public void Begin()
            {
                if (version == int.MaxValue)
                {
                    Array.Clear(nodeState, 0, nodeState.Length);
                    version = 1;
                }
                else
                {
                    version++;
                }

                heap.Init(fScore);
            }

            public void PrepareNode(int node)
            {
                if (nodeState[node] == version || nodeState[node] == -version) return;
                nodeState[node] = version;
                gScore[node] = LFloat.MaxValue;
                fScore[node] = LFloat.MaxValue;
                parent[node] = -1;
                parentLink[node] = null;
            }

        }

        private readonly List<Triangle> triangles;
        private readonly LVector3[] cachedCenters;
        private readonly Dictionary<Triangle, int> triangleIndices;
        private readonly Dictionary<ulong, Edge> sharedEdges;
        private readonly Transition[][] transitions;
        private readonly BvhNode root;
        private readonly bool useHeuristic;
        private readonly Stack<SearchContext> contextPool = new Stack<SearchContext>();
        private readonly object contextPoolLock = new object();

        public LVector3 boundEps = new LVector3(
            LFloat.FromRaw(100000L),
            LFloat.FromRaw(100000L),
            LFloat.FromRaw(100000L)); // 0.1

        public NavMap(NavData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            triangles = data.triangles == null
                ? new List<Triangle>()
                : new List<Triangle>(data.triangles);
            cachedCenters = new LVector3[triangles.Count];
            triangleIndices = new Dictionary<Triangle, int>(triangles.Count);
            sharedEdges = new Dictionary<ulong, Edge>();
            transitions = new Transition[triangles.Count][];

            bool metricLinks = true;
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle triangle = triangles[i];
                if (triangle == null)
                    throw new ArgumentException("NavData contains a null triangle.", nameof(data));

                if (triangleIndices.ContainsKey(triangle))
                    throw new ArgumentException("NavData contains a duplicate triangle reference.", nameof(data));
                triangle.neighbors = triangle.neighbors ?? new List<int>();
                triangle.links = triangle.links ?? new List<TriangleLink>();
                triangleIndices.Add(triangle, i);
                cachedCenters[i] = triangle.GetInteriorPoint();

                for (int j = 0; j < triangle.links.Count; j++)
                {
                    TriangleLink link = triangle.links[j];
                    if (link == null)
                        throw new ArgumentException("NavData contains a null triangle link.", nameof(data));
                    if (link.neighbor >= 0 && link.neighbor < triangles.Count &&
                        link.neighbor != i &&
                        link.cost < NavHelper.Heuristic(link.from, link.to))
                        metricLinks = false;
                }
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle triangle = triangles[i];
                int transitionCount = 0;
                for (int j = 0; j < triangle.neighbors.Count; j++)
                {
                    int neighbor = triangle.neighbors[j];
                    if (neighbor < 0 || neighbor >= triangles.Count || neighbor == i) continue;
                    transitionCount++;
                    ulong key = GetTrianglePairKey(i, neighbor);
                    if (sharedEdges.ContainsKey(key)) continue;
                    if (triangle.GetSharedEdge(triangles[neighbor], out Edge edge))
                        sharedEdges[key] = edge;
                }

                for (int j = 0; j < triangle.links.Count; j++)
                {
                    int neighbor = triangle.links[j].neighbor;
                    if (neighbor >= 0 && neighbor < triangles.Count && neighbor != i)
                        transitionCount++;
                }

                Transition[] triangleTransitions = new Transition[transitionCount];
                int transitionIndex = 0;
                for (int j = 0; j < triangle.neighbors.Count; j++)
                {
                    int neighbor = triangle.neighbors[j];
                    if (neighbor < 0 || neighbor >= triangles.Count || neighbor == i) continue;
                    triangleTransitions[transitionIndex++] = new Transition(
                        neighbor,
                        NavHelper.Heuristic(cachedCenters[i], cachedCenters[neighbor]),
                        null);
                }

                for (int j = 0; j < triangle.links.Count; j++)
                {
                    TriangleLink link = triangle.links[j];
                    int neighbor = link.neighbor;
                    if (neighbor < 0 || neighbor >= triangles.Count || neighbor == i) continue;

                    LFloat linkCost =
                        NavHelper.Heuristic(cachedCenters[i], link.from) +
                        LMath.Max(link.cost, LFloat.zero) +
                        NavHelper.Heuristic(link.to, cachedCenters[neighbor]);
                    triangleTransitions[transitionIndex++] = new Transition(neighbor, linkCost, link);
                }
                transitions[i] = triangleTransitions;
            }
            root = BvhNode.Build(triangles);
            useHeuristic = metricLinks;
        }

        /// <summary>搜索并平滑路径。result 会先清空；成功时至少包含 Start 和 End。</summary>
        public NavResult Search(LVector3 start, LVector3 end, List<NavPathPoint> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            if (triangles.Count == 0) return NavResult.NavDataErr;

            SearchContext context;
            lock (contextPoolLock)
            {
                context = contextPool.Count > 0
                    ? contextPool.Pop()
                    : new SearchContext(triangles.Count);
            }
            try
            {
                context.Begin();
                NavResult searchResult = FindPath(context, ref start, ref end);
                if (searchResult != NavResult.Success)
                    return searchResult;
                if (SmoothPath(context, start, end, result))
                    return NavResult.Success;

                result.Clear();
                return NavResult.NavDataErr;
            }
            finally
            {
                context.trianglePath.Clear();
                context.portals.Clear();
                context.funnelResult.Clear();
                lock (contextPoolLock)
                    contextPool.Push(context);
            }
        }

        /// <summary>通过 BVH 定位点所在导航三角形，并把点吸附到该三角形平面。</summary>
        public bool TryGetTriangle(
            LVector3 point,
            out Triangle triangle,
            out LVector3 snappedPoint)
        {
            triangle = null;
            snappedPoint = point;
            return root != null &&
                   root.TryGetTriangle(point, boundEps, out triangle, out snappedPoint);
        }

        private static ulong GetTrianglePairKey(int a, int b)
        {
            uint min = (uint)(a < b ? a : b);
            uint max = (uint)(a < b ? b : a);
            return ((ulong)min << 32) | max;
        }

        private static LFloat SignedArea(LVector2 a, LVector2 b, LVector2 c)
        {
            return LVector2.Cross(b - a, c - a);
        }

        private static bool SamePoint(LVector2 a, LVector2 b)
        {
            return LMath.Abs(a.x - b.x) <= NavHelper.epsilon &&
                   LMath.Abs(a.y - b.y) <= NavHelper.epsilon;
        }

        private LFloat GetHeuristic(int node, LVector3 endCenter)
        {
            return useHeuristic
                ? NavHelper.Heuristic(cachedCenters[node], endCenter)
                : LFloat.zero;
        }

        private void Relax(
            SearchContext context,
            int current,
            int neighbor,
            LFloat edgeCost,
            TriangleLink link,
            LVector3 endCenter)
        {
            if (neighbor < 0 || neighbor >= triangles.Count ||
                context.nodeState[neighbor] == -context.version)
                return;

            context.PrepareNode(neighbor);
            LFloat tentativeG = context.gScore[current] + edgeCost;
            if (tentativeG >= context.gScore[neighbor]) return;

            context.parent[neighbor] = current;
            context.parentLink[neighbor] = link;
            context.gScore[neighbor] = tentativeG;
            context.fScore[neighbor] = tentativeG + GetHeuristic(neighbor, endCenter);
            context.heap.Push(neighbor);
        }

        /// <summary>在共享边和 TriangleLink 两类转移上运行 A*，并反向重建三角形走廊。</summary>
        private NavResult FindPath(SearchContext context, ref LVector3 start, ref LVector3 end)
        {
            if (!TryGetTriangle(start, out Triangle startTriangle, out LVector3 snappedStart))
                return NavResult.StartNotInNavMesh;
            start = snappedStart;
            if (!TryGetTriangle(end, out Triangle endTriangle, out LVector3 snappedEnd))
                return NavResult.EndNotInNavMesh;
            end = snappedEnd;

            if (!triangleIndices.TryGetValue(startTriangle, out int startIndex) ||
                !triangleIndices.TryGetValue(endTriangle, out int endIndex))
                return NavResult.NavDataErr;
            if (startIndex == endIndex)
            {
                context.trianglePath.Add(new TrianglePathStep(startIndex, null));
                return NavResult.Success;
            }

            LVector3 endCenter = cachedCenters[endIndex];
            context.PrepareNode(startIndex);
            context.gScore[startIndex] = LFloat.zero;
            context.fScore[startIndex] = GetHeuristic(startIndex, endCenter);
            context.heap.Push(startIndex);

            while (context.heap.Count > 0)
            {
                int current = context.heap.Pop();
                if (context.nodeState[current] == -context.version) continue;
                if (current == endIndex)
                {
                    context.trianglePath.Clear();
                    int pathNode = endIndex;
                    while (true)
                    {
                        context.trianglePath.Add(new TrianglePathStep(
                            pathNode,
                            pathNode == startIndex ? null : context.parentLink[pathNode]));
                        if (pathNode == startIndex)
                            break;

                        pathNode = context.parent[pathNode];
                        if (pathNode == -1)
                        {
                            context.trianglePath.Clear();
                            return NavResult.NotFound;
                        }
                    }
                    context.trianglePath.Reverse();
                    return NavResult.Success;
                }

                context.nodeState[current] = -context.version;
                Transition[] currentTransitions = transitions[current];
                for (int i = 0; i < currentTransitions.Length; i++)
                {
                    Transition transition = currentTransitions[i];
                    Relax(
                        context,
                        current,
                        transition.neighbor,
                        transition.cost,
                        transition.link,
                        endCenter);
                }
            }

            return NavResult.NotFound;
        }

        private static bool TryCreateInitialUnfolded(
            Triangle triangle,
            out UnfoldedTriangle unfolded)
        {
            LFloat distance12 = NavHelper.Heuristic(triangle.point1, triangle.point2);
            if (distance12 <= NavHelper.epsilon)
            {
                unfolded = default;
                return false;
            }

            LFloat distance13 = NavHelper.Heuristic(triangle.point1, triangle.point3);
            LFloat distance23 = NavHelper.Heuristic(triangle.point2, triangle.point3);
            LFloat distance12Sq = distance12 * distance12;
            LFloat distance13Sq = distance13 * distance13;
            LFloat distance23Sq = distance23 * distance23;
            LFloat x =
                (distance13Sq + distance12Sq - distance23Sq) /
                (LFloat.two * distance12);
            LFloat ySq = distance13Sq - x * x;
            if (ySq < -NavHelper.epsilon)
            {
                unfolded = default;
                return false;
            }

            LFloat y = LMath.Sqrt(LMath.Max(ySq, LFloat.zero));
            unfolded = new UnfoldedTriangle(
                triangle,
                LVector2.zero,
                new LVector2(distance12, LFloat.zero),
                new LVector2(x, y));
            return true;
        }

        /// <summary>以共享边为铰链，把下一个三角形等距展开到当前二维走廊平面。</summary>
        private static bool TryUnfoldNext(
            UnfoldedTriangle current,
            Triangle nextTriangle,
            Edge sharedEdge,
            out UnfoldedTriangle next)
        {
            if (!TryMapPoint(current, sharedEdge.a, out LVector2 edgeA) ||
                !TryMapPoint(current, sharedEdge.b, out LVector2 edgeB))
            {
                next = default;
                return false;
            }

            LVector2 edge = edgeB - edgeA;
            LFloat edgeLength = edge.magnitude;
            if (edgeLength <= NavHelper.epsilon)
            {
                next = default;
                return false;
            }

            // Portal 可能只是当前长边的一部分，端点未必是任一三角形的顶点。
            // 使用三角形内部点判断展开侧，并分别按到 Portal 两端的距离映射下一个三角形全部顶点。
            LFloat currentSide = SignedArea(edgeA, edgeB, current.center);
            if (LMath.Abs(currentSide) <= NavHelper.epsilon)
            {
                next = default;
                return false;
            }

            bool usePositiveSide = currentSide < LFloat.zero;
            if (!TryMapAcrossEdge(
                    nextTriangle.point1, sharedEdge, edgeA, edgeB, edgeLength,
                    usePositiveSide, out LVector2 point1) ||
                !TryMapAcrossEdge(
                    nextTriangle.point2, sharedEdge, edgeA, edgeB, edgeLength,
                    usePositiveSide, out LVector2 point2) ||
                !TryMapAcrossEdge(
                    nextTriangle.point3, sharedEdge, edgeA, edgeB, edgeLength,
                    usePositiveSide, out LVector2 point3))
            {
                next = default;
                return false;
            }

            next = new UnfoldedTriangle(nextTriangle, point1, point2, point3);
            return true;
        }

        private static bool TryMapAcrossEdge(
            LVector3 point,
            Edge sharedEdge,
            LVector2 edgeA,
            LVector2 edgeB,
            LFloat edgeLength,
            bool positiveSide,
            out LVector2 mapped)
        {
            LVector3 sourceEdge = sharedEdge.b - sharedEdge.a;
            LFloat sourceLengthSquared = LVector3.Dot(sourceEdge, sourceEdge);
            if (sourceLengthSquared <= LFloat.EPSILON)
            {
                mapped = default;
                return false;
            }

            // 不用余弦定理的“两个大距离平方相减”反推高度：当 Portal 只是长边中间的一小段、
            // 待映射顶点位于 Portal 延长线很远处时，该公式会放大定点舍入误差。
            // 投影参数给出沿 Portal 的有符号位置，叉积直接给出点到 Portal 直线的垂距。
            LVector3 offset = point - sharedEdge.a;
            LFloat alongRatio = LVector3.Dot(offset, sourceEdge) / sourceLengthSquared;
            LFloat sourceLength = LMath.Sqrt(sourceLengthSquared);
            LFloat height = LVector3.Cross(sourceEdge, offset).magnitude / sourceLength;
            LVector2 edge = edgeB - edgeA;
            LFloat unitX = edge.x / edgeLength;
            LFloat unitY = edge.y / edgeLength;
            LFloat side = positiveSide ? LFloat.one : -LFloat.one;
            mapped = new LVector2(
                edgeA.x + edge.x * alongRatio - unitY * height * side,
                edgeA.y + edge.y * alongRatio + unitX * height * side);
            return true;
        }

        private static bool TryMapPoint(
            UnfoldedTriangle unfolded,
            LVector3 point,
            out LVector2 mapped)
        {
            LVector3 v0 = unfolded.triangle.point2 - unfolded.triangle.point1;
            LVector3 v1 = unfolded.triangle.point3 - unfolded.triangle.point1;
            LVector3 v2 = point - unfolded.triangle.point1;
            LFloat dot00 = LVector3.Dot(v0, v0);
            LFloat dot01 = LVector3.Dot(v0, v1);
            LFloat dot02 = LVector3.Dot(v0, v2);
            LFloat dot11 = LVector3.Dot(v1, v1);
            LFloat dot12 = LVector3.Dot(v1, v2);
            LFloat denominator = dot00 * dot11 - dot01 * dot01;
            if (LMath.Abs(denominator) <= LFloat.EPSILON)
            {
                mapped = default;
                return false;
            }

            LFloat point2Weight = (dot11 * dot02 - dot01 * dot12) / denominator;
            LFloat point3Weight = (dot00 * dot12 - dot01 * dot02) / denominator;
            mapped = new LVector2(
                unfolded.point1.x +
                (unfolded.point2.x - unfolded.point1.x) * point2Weight +
                (unfolded.point3.x - unfolded.point1.x) * point3Weight,
                unfolded.point1.y +
                (unfolded.point2.y - unfolded.point1.y) * point2Weight +
                (unfolded.point3.y - unfolded.point1.y) * point3Weight);
            return true;
        }

        private bool TryBuildUnfoldedCorridor(
            SearchContext context,
            List<TrianglePathStep> path,
            int startIndex,
            int endIndex,
            LVector3 start,
            LVector3 end,
            out LVector2 startUnfolded,
            out LVector2 endUnfolded)
        {
            context.portals.Clear();
            if (!TryCreateInitialUnfolded(
                    triangles[path[startIndex].index],
                    out UnfoldedTriangle current))
            {
                startUnfolded = default;
                endUnfolded = default;
                return false;
            }

            if (!TryMapPoint(current, start, out startUnfolded))
            {
                endUnfolded = default;
                return false;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                TrianglePathStep currentStep = path[i];
                TrianglePathStep nextStep = path[i + 1];
                if (!sharedEdges.TryGetValue(
                        GetTrianglePairKey(currentStep.index, nextStep.index),
                        out Edge sharedEdge) ||
                    !TryMapPoint(current, sharedEdge.a, out LVector2 edgeA) ||
                    !TryMapPoint(current, sharedEdge.b, out LVector2 edgeB) ||
                    !TryUnfoldNext(
                        current,
                        triangles[nextStep.index],
                        sharedEdge,
                        out UnfoldedTriangle next))
                {
                    endUnfolded = default;
                    return false;
                }

                LVector2 midpoint = new LVector2(
                    (edgeA.x + edgeB.x) / 2,
                    (edgeA.y + edgeB.y) / 2);
                if (SignedArea(
                        midpoint,
                        midpoint + next.center - current.center,
                        edgeA) >= LFloat.zero)
                    context.portals.Add(new Portal(
                        sharedEdge.a,
                        sharedEdge.b,
                        edgeA,
                        edgeB));
                else
                    context.portals.Add(new Portal(
                        sharedEdge.b,
                        sharedEdge.a,
                        edgeB,
                        edgeA));

                current = next;
            }

            return TryMapPoint(current, end, out endUnfolded);
        }

        /// <summary>
        /// 在有序左右 Portal 上收紧漏斗；左右边越过时输出对侧顶点并以该点重启漏斗。
        /// </summary>
        private static void FunnelAlgorithm2D(
            LVector3 start,
            LVector2 startUnfolded,
            LVector3 end,
            LVector2 endUnfolded,
            List<Portal> portals,
            List<LVector3> result)
        {
            result.Clear();
            result.Add(start);
            LVector2 lastResultUnfolded = startUnfolded;
            LVector2 apex = startUnfolded;
            LVector2 left = apex;
            LVector2 right = apex;
            LVector3 leftPosition = start;
            LVector3 rightPosition = start;
            int apexIndex = -1;
            int leftIndex = -1;
            int rightIndex = -1;
            int portalIndex = 0;
            while (portalIndex <= portals.Count)
            {
                LVector2 newLeft;
                LVector2 newRight;
                LVector3 newLeftPosition;
                LVector3 newRightPosition;
                if (portalIndex == portals.Count)
                {
                    newLeft = endUnfolded;
                    newRight = endUnfolded;
                    newLeftPosition = end;
                    newRightPosition = end;
                }
                else
                {
                    Portal portal = portals[portalIndex];
                    newLeft = portal.left;
                    newRight = portal.right;
                    newLeftPosition = portal.leftPosition;
                    newRightPosition = portal.rightPosition;
                }

                if (SignedArea(apex, right, newRight) >= LFloat.zero)
                {
                    if (SamePoint(apex, right) ||
                        SignedArea(apex, left, newRight) < LFloat.zero)
                    {
                        right = newRight;
                        rightPosition = newRightPosition;
                        rightIndex = portalIndex;
                    }
                    else
                    {
                        if (!SamePoint(lastResultUnfolded, left))
                        {
                            result.Add(leftPosition);
                            lastResultUnfolded = left;
                        }
                        apex = left;
                        apexIndex = leftIndex;
                        left = apex;
                        right = apex;
                        rightPosition = leftPosition;
                        leftIndex = apexIndex;
                        rightIndex = apexIndex;
                        portalIndex = apexIndex + 1;
                        continue;
                    }
                }

                if (SignedArea(apex, left, newLeft) <= LFloat.zero)
                {
                    if (SamePoint(apex, left) ||
                        SignedArea(apex, right, newLeft) > LFloat.zero)
                    {
                        left = newLeft;
                        leftPosition = newLeftPosition;
                        leftIndex = portalIndex;
                    }
                    else
                    {
                        if (!SamePoint(lastResultUnfolded, right))
                        {
                            result.Add(rightPosition);
                            lastResultUnfolded = right;
                        }
                        apex = right;
                        apexIndex = rightIndex;
                        left = apex;
                        right = apex;
                        leftPosition = rightPosition;
                        leftIndex = apexIndex;
                        rightIndex = apexIndex;
                        portalIndex = apexIndex + 1;
                        continue;
                    }
                }

                portalIndex++;
            }

            if (!SamePoint(lastResultUnfolded, endUnfolded))
                result.Add(end);
        }

        /// <summary>按 Link 把走廊分段，每段独立展开并漏斗平滑，再拼接 LinkFrom/LinkTo。</summary>
        private bool SmoothPath(
            SearchContext context,
            LVector3 start,
            LVector3 end,
            List<NavPathPoint> result)
        {
            List<TrianglePathStep> path = context.trianglePath;
            if (path.Count == 1)
            {
                Triangle triangle = triangles[path[0].index];
                result.Add(new NavPathPoint(PointType.Start, triangle.Snap(start)));
                result.Add(new NavPathPoint(PointType.End, triangle.Snap(end)));
                return true;
            }

            int index = 0;
            while (index != path.Count - 1)
            {
                int startIndex = index;
                while (index < path.Count - 1 && path[index + 1].incomingLink == null)
                    index++;

                bool readyEnd = index == path.Count - 1;
                LVector3 segmentEnd = end;
                LVector3 nextStart = start;
                if (!readyEnd)
                {
                    TriangleLink link = path[index + 1].incomingLink;
                    if (link == null) return false;
                    segmentEnd = triangles[path[index].index].Snap(link.from);
                    nextStart = triangles[path[index + 1].index].Snap(link.to);
                }

                if (!TryBuildUnfoldedCorridor(
                        context,
                        path,
                        startIndex,
                        index,
                        start,
                        segmentEnd,
                        out LVector2 startUnfolded,
                        out LVector2 endUnfolded))
                    return false;

                FunnelAlgorithm2D(
                    start,
                    startUnfolded,
                    segmentEnd,
                    endUnfolded,
                    context.portals,
                    context.funnelResult);

                for (int i = 0; i < context.funnelResult.Count; i++)
                {
                    PointType type = PointType.Point;
                    if (i == 0)
                        type = startIndex == 0 ? PointType.Start : PointType.LinkTo;
                    else if (i == context.funnelResult.Count - 1)
                        type = readyEnd ? PointType.End : PointType.LinkFrom;
                    result.Add(new NavPathPoint(type, context.funnelResult[i]));
                }

                if (!readyEnd)
                {
                    start = nextStart;
                    index++;
                    if (index == path.Count - 1)
                    {
                        Triangle finalTriangle = triangles[path[index].index];
                        result.Add(new NavPathPoint(PointType.LinkTo, finalTriangle.Snap(nextStart)));
                        result.Add(new NavPathPoint(PointType.End, finalTriangle.Snap(end)));
                    }
                }
            }

            return true;
        }
    }
}
