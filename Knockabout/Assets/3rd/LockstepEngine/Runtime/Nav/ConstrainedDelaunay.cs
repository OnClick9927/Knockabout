using System;
using System.Collections.Generic;
using System.Numerics;

namespace Lockstep.Nav
{
    /// <summary>
    /// 面向导航轮廓的确定性约束德洛内三角剖分器。
    /// <para>输入由一个逆时针外轮廓和若干顺时针孔洞组成。算法先用可见桥把孔洞接入外轮廓，
    /// 再执行耳切得到合法初始剖分，最后对所有非约束公共边执行德洛内翻边。</para>
    /// <para>轮廓边和孔洞桥均是约束边，永远不会被翻转，因此三角形不会跨越障碍、孔洞或区域边界。
    /// 内接圆和方向判断直接使用定点原始整数与 BigInteger，避免大坐标下溢出及浮点平台差异。</para>
    /// </summary>
    internal static class ConstrainedDelaunay
    {
        internal readonly struct IndexTriangle
        {
            public readonly int a;
            public readonly int b;
            public readonly int c;

            public IndexTriangle(int a, int b, int c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            private readonly long x;
            private readonly long y;
            private readonly long z;

            public VertexKey(LVector3 point)
            {
                x = point._x;
                y = point._y;
                z = point._z;
            }

            public bool Equals(VertexKey other) => x == other.x && y == other.y && z == other.z;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);

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

        private readonly struct IndexEdge : IEquatable<IndexEdge>
        {
            public readonly int a;
            public readonly int b;

            public IndexEdge(int a, int b)
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

            public bool Equals(IndexEdge other) => a == other.a && b == other.b;
            public override bool Equals(object obj) => obj is IndexEdge other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return a * 397 ^ b;
                }
            }
        }

        private sealed class Node
        {
            public int vertex;
            public Node previous;
            public Node next;

            public Node(int vertex)
            {
                this.vertex = vertex;
            }
        }

        private readonly struct EdgeOwners
        {
            public readonly int first;
            public readonly int second;

            public EdgeOwners(int first, int second = -1)
            {
                this.first = first;
                this.second = second;
            }
        }

        /// <summary>
        /// 对轮廓执行约束德洛内剖分。退化、自接触或无法可靠桥接的输入返回 false，
        /// 调用方应保留原来的安全剖分作为回退结果。
        /// </summary>
        public static bool TryTriangulate(
            IList<List<LVector3>> loops,
            out List<LVector3> vertices,
            out List<IndexTriangle> triangles)
        {
            vertices = new List<LVector3>();
            triangles = new List<IndexTriangle>();
            if (loops == null || loops.Count == 0 || loops[0] == null || loops[0].Count < 3)
                return false;

            var vertexIndices = new Dictionary<VertexKey, int>();
            var constraints = new HashSet<IndexEdge>();
            Node outer = BuildRing(
                loops[0],
                true,
                vertices,
                vertexIndices,
                constraints);
            if (outer == null) return false;

            var holes = new List<Node>();
            for (int i = 1; i < loops.Count; i++)
            {
                if (loops[i] == null || loops[i].Count < 3) continue;
                Node hole = BuildRing(
                    loops[i],
                    false,
                    vertices,
                    vertexIndices,
                    constraints);
                if (hole == null) return false;
                holes.Add(GetRightmost(hole, vertices));
            }

            // 从右向左处理孔洞，右侧孔洞先并入当前外环；后续射线可以把已合并的桥视为外环的一部分。
            // 不使用捕获 out vertices 的比较器闭包：除规避 C# out 参数限制外，显式插入排序
            // 在孔洞数量通常很少的前提下更容易保证相等键的处理顺序完全确定。
            for (int i = 1; i < holes.Count; i++)
            {
                Node value = holes[i];
                int insert = i;
                while (insert > 0 &&
                       CompareRightmost(value, holes[insert - 1], vertices) < 0)
                {
                    holes[insert] = holes[insert - 1];
                    insert--;
                }
                holes[insert] = value;
            }
            for (int i = 0; i < holes.Count; i++)
            {
                Node bridge = FindRightBridge(
                    outer,
                    holes[i],
                    vertices,
                    vertexIndices,
                    constraints);
                if (bridge == null) return false;

                constraints.Add(new IndexEdge(bridge.vertex, holes[i].vertex));
                outer = JoinRings(bridge, holes[i]);
            }

            if (!EarClip(outer, vertices, triangles))
            {
                triangles.Clear();
                return false;
            }

            Legalize(vertices, triangles, constraints);
            return triangles.Count > 0;
        }

        private static int CompareRightmost(Node left, Node right, List<LVector3> vertices)
        {
            LVector3 a = vertices[left.vertex];
            LVector3 b = vertices[right.vertex];
            int result = b._x.CompareTo(a._x);
            if (result != 0) return result;
            return a._z.CompareTo(b._z);
        }

        private static Node BuildRing(
            List<LVector3> source,
            bool counterClockwise,
            List<LVector3> vertices,
            Dictionary<VertexKey, int> vertexIndices,
            HashSet<IndexEdge> constraints)
        {
            var cleaned = new List<LVector3>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                LVector3 point = source[i];
                if (cleaned.Count == 0 || !NavHelper.SamePoint(cleaned[cleaned.Count - 1], point))
                    cleaned.Add(point);
            }
            if (cleaned.Count > 1 && NavHelper.SamePoint(cleaned[0], cleaned[cleaned.Count - 1]))
                cleaned.RemoveAt(cleaned.Count - 1);
            RemoveCollinear(cleaned);
            if (cleaned.Count < 3) return null;

            bool isCounterClockwise = SignedArea(cleaned) > BigInteger.Zero;
            if (isCounterClockwise != counterClockwise)
                cleaned.Reverse();

            Node first = null;
            Node previous = null;
            for (int i = 0; i < cleaned.Count; i++)
            {
                int vertex = GetVertex(cleaned[i], vertices, vertexIndices);
                var node = new Node(vertex);
                if (first == null) first = node;
                if (previous != null)
                {
                    previous.next = node;
                    node.previous = previous;
                }
                previous = node;
            }
            previous.next = first;
            first.previous = previous;

            Node current = first;
            do
            {
                constraints.Add(new IndexEdge(current.vertex, current.next.vertex));
                current = current.next;
            } while (!ReferenceEquals(current, first));
            return first;
        }

        private static void RemoveCollinear(List<LVector3> points)
        {
            bool changed = true;
            while (changed && points.Count >= 3)
            {
                changed = false;
                for (int i = 0; i < points.Count; i++)
                {
                    LVector3 previous = points[(i + points.Count - 1) % points.Count];
                    LVector3 current = points[i];
                    LVector3 next = points[(i + 1) % points.Count];
                    if (Orient(previous, current, next) != BigInteger.Zero) continue;

                    points.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        private static int GetVertex(
            LVector3 point,
            List<LVector3> vertices,
            Dictionary<VertexKey, int> vertexIndices)
        {
            var key = new VertexKey(point);
            if (vertexIndices.TryGetValue(key, out int index)) return index;

            index = vertices.Count;
            vertices.Add(point);
            vertexIndices.Add(key, index);
            return index;
        }

        private static Node GetRightmost(Node ring, List<LVector3> vertices)
        {
            Node result = ring;
            Node current = ring.next;
            while (!ReferenceEquals(current, ring))
            {
                LVector3 point = vertices[current.vertex];
                LVector3 best = vertices[result.vertex];
                if (point._x > best._x || point._x == best._x && point._z < best._z)
                    result = current;
                current = current.next;
            }
            return result;
        }

        /// <summary>
        /// 从孔洞最右点向 +X 发射水平射线，取第一次命中的当前外环边。
        /// 命中边内部时会显式插入交点，使桥端点成为真正的约束顶点。
        /// </summary>
        private static Node FindRightBridge(
            Node outer,
            Node hole,
            List<LVector3> vertices,
            Dictionary<VertexKey, int> vertexIndices,
            HashSet<IndexEdge> constraints)
        {
            LVector3 holePoint = vertices[hole.vertex];
            Node bestEdgeStart = null;
            LFloat bestX = LFloat.MaxValue;
            LFloat bestRatio = LFloat.zero;

            Node current = outer;
            do
            {
                LVector3 a = vertices[current.vertex];
                LVector3 b = vertices[current.next.vertex];
                bool crosses =
                    (a.z <= holePoint.z && b.z > holePoint.z) ||
                    (b.z <= holePoint.z && a.z > holePoint.z);
                if (crosses)
                {
                    LFloat ratio = (holePoint.z - a.z) / (b.z - a.z);
                    LFloat x = a.x + (b.x - a.x) * ratio;
                    if (x >= holePoint.x - NavHelper.epsilon && x < bestX)
                    {
                        bestX = x;
                        bestRatio = ratio;
                        bestEdgeStart = current;
                    }
                }
                current = current.next;
            } while (!ReferenceEquals(current, outer));

            if (bestEdgeStart == null) return null;
            LVector3 edgeA = vertices[bestEdgeStart.vertex];
            LVector3 edgeB = vertices[bestEdgeStart.next.vertex];

            // 优先复用命中边的可见端点，避免为了孔洞桥额外增加 Steiner 顶点。
            // 对矩形孔洞等常见轮廓，这能达到 n + 2h - 2 的最少三角形数量。
            Node firstCandidate = CompareBridgeCandidate(edgeA, edgeB, holePoint) <= 0
                ? bestEdgeStart
                : bestEdgeStart.next;
            Node secondCandidate = ReferenceEquals(firstCandidate, bestEdgeStart)
                ? bestEdgeStart.next
                : bestEdgeStart;
            if (CanBridge(outer, hole, firstCandidate, vertices)) return firstCandidate;
            if (CanBridge(outer, hole, secondCandidate, vertices)) return secondCandidate;

            LVector3 intersection = new LVector3(
                bestX,
                edgeA.y + (edgeB.y - edgeA.y) * bestRatio,
                holePoint.z);
            if (NavHelper.SamePoint(intersection, edgeA)) return bestEdgeStart;
            if (NavHelper.SamePoint(intersection, edgeB)) return bestEdgeStart.next;

            int vertex = GetVertex(intersection, vertices, vertexIndices);
            constraints.Remove(new IndexEdge(bestEdgeStart.vertex, bestEdgeStart.next.vertex));
            constraints.Add(new IndexEdge(bestEdgeStart.vertex, vertex));
            constraints.Add(new IndexEdge(vertex, bestEdgeStart.next.vertex));
            var inserted = new Node(vertex)
            {
                previous = bestEdgeStart,
                next = bestEdgeStart.next
            };
            bestEdgeStart.next.previous = inserted;
            bestEdgeStart.next = inserted;
            return inserted;
        }

        private static int CompareBridgeCandidate(
            LVector3 left,
            LVector3 right,
            LVector3 hole)
        {
            // 更靠右的端点优先；X 相同时选择离孔洞更近的端点，最后以 Z 保证稳定顺序。
            int result = right._x.CompareTo(left._x);
            if (result != 0) return result;
            BigInteger leftX = (BigInteger)left._x - hole._x;
            BigInteger leftZ = (BigInteger)left._z - hole._z;
            BigInteger rightX = (BigInteger)right._x - hole._x;
            BigInteger rightZ = (BigInteger)right._z - hole._z;
            BigInteger leftDistance =
                leftX * leftX + leftZ * leftZ;
            BigInteger rightDistance =
                rightX * rightX + rightZ * rightZ;
            result = leftDistance.CompareTo(rightDistance);
            return result != 0 ? result : left._z.CompareTo(right._z);
        }

        private static bool CanBridge(
            Node outer,
            Node hole,
            Node candidate,
            List<LVector3> vertices)
        {
            LVector3 a = vertices[hole.vertex];
            LVector3 b = vertices[candidate.vertex];
            if (NavHelper.SamePoint(a, b)) return false;
            if (IntersectsRingProperly(a, b, outer, candidate.vertex, vertices) ||
                IntersectsRingProperly(a, b, hole, hole.vertex, vertices))
                return false;

            LVector3 midpoint = new LVector3(
                (a.x + b.x) / 2,
                (a.y + b.y) / 2,
                (a.z + b.z) / 2);
            return IsPointInsideRing(midpoint, outer, vertices);
        }

        private static bool IntersectsRingProperly(
            LVector3 a,
            LVector3 b,
            Node ring,
            int allowedVertex,
            List<LVector3> vertices)
        {
            Node current = ring;
            do
            {
                int edgeAIndex = current.vertex;
                int edgeBIndex = current.next.vertex;
                if (edgeAIndex != allowedVertex && edgeBIndex != allowedVertex &&
                    SegmentsProperlyIntersect(
                        a,
                        b,
                        vertices[edgeAIndex],
                        vertices[edgeBIndex]))
                    return true;
                current = current.next;
            } while (!ReferenceEquals(current, ring));
            return false;
        }

        private static bool SegmentsProperlyIntersect(
            LVector3 a,
            LVector3 b,
            LVector3 c,
            LVector3 d)
        {
            BigInteger abC = Orient(a, b, c);
            BigInteger abD = Orient(a, b, d);
            BigInteger cdA = Orient(c, d, a);
            BigInteger cdB = Orient(c, d, b);
            return abC != BigInteger.Zero &&
                   abD != BigInteger.Zero &&
                   cdA != BigInteger.Zero &&
                   cdB != BigInteger.Zero &&
                   (abC > 0) != (abD > 0) &&
                   (cdA > 0) != (cdB > 0);
        }

        private static bool IsPointInsideRing(
            LVector3 point,
            Node ring,
            List<LVector3> vertices)
        {
            bool inside = false;
            Node current = ring;
            do
            {
                LVector3 a = vertices[current.vertex];
                LVector3 b = vertices[current.next.vertex];
                if ((a.z > point.z) != (b.z > point.z))
                {
                    LFloat intersectionX =
                        a.x + (point.z - a.z) * (b.x - a.x) / (b.z - a.z);
                    if (intersectionX > point.x) inside = !inside;
                }
                current = current.next;
            } while (!ReferenceEquals(current, ring));
            return inside;
        }

        /// <summary>
        /// 用两条方向相反、几何位置相同的桥把孔洞环拼入外环。
        /// 复制桥端节点是耳切表示带孔多边形的常用方式，顶点索引仍共享，不增加序列化顶点。
        /// </summary>
        private static Node JoinRings(Node outer, Node hole)
        {
            Node outerNext = outer.next;
            Node holePrevious = hole.previous;
            var outerCopy = new Node(outer.vertex);
            var holeCopy = new Node(hole.vertex);

            outer.next = hole;
            hole.previous = outer;

            holePrevious.next = holeCopy;
            holeCopy.previous = holePrevious;
            holeCopy.next = outerCopy;
            outerCopy.previous = holeCopy;
            outerCopy.next = outerNext;
            outerNext.previous = outerCopy;
            return outer;
        }

        private static bool EarClip(
            Node ring,
            List<LVector3> vertices,
            List<IndexTriangle> triangles)
        {
            int remaining = CountNodes(ring);
            if (remaining < 3) return false;

            Node current = ring;
            int failedCandidates = 0;
            while (remaining > 3)
            {
                if (IsEar(current, ring, vertices))
                {
                    AddCounterClockwiseTriangle(
                        triangles,
                        current.previous.vertex,
                        current.vertex,
                        current.next.vertex,
                        vertices);
                    Node next = current.next;
                    RemoveNode(current);
                    current = next;
                    remaining--;
                    failedCandidates = 0;
                    ring = current;
                    continue;
                }

                current = current.next;
                failedCandidates++;
                if (failedCandidates <= remaining) continue;

                // 桥接后的弱简单多边形可能留下相邻共线点；先删去不承载面积的点再继续。
                Node removable = FindRemovableCollinear(ring, vertices);
                if (removable == null || remaining <= 3)
                    return false;
                if (ReferenceEquals(removable, ring)) ring = removable.next;
                if (ReferenceEquals(removable, current)) current = removable.next;
                RemoveNode(removable);
                remaining--;
                failedCandidates = 0;
            }

            AddCounterClockwiseTriangle(
                triangles,
                ring.vertex,
                ring.next.vertex,
                ring.next.next.vertex,
                vertices);
            return triangles.Count > 0;
        }

        private static bool IsEar(Node ear, Node ring, List<LVector3> vertices)
        {
            LVector3 a = vertices[ear.previous.vertex];
            LVector3 b = vertices[ear.vertex];
            LVector3 c = vertices[ear.next.vertex];
            if (Orient(a, b, c) <= BigInteger.Zero) return false;

            Node point = ring;
            do
            {
                if (!ReferenceEquals(point, ear.previous) &&
                    !ReferenceEquals(point, ear) &&
                    !ReferenceEquals(point, ear.next) &&
                    point.vertex != ear.previous.vertex &&
                    point.vertex != ear.vertex &&
                    point.vertex != ear.next.vertex &&
                    PointStrictlyInsideTriangle(vertices[point.vertex], a, b, c))
                    return false;
                point = point.next;
            } while (!ReferenceEquals(point, ring));
            return true;
        }

        private static bool PointStrictlyInsideTriangle(
            LVector3 point,
            LVector3 a,
            LVector3 b,
            LVector3 c)
        {
            BigInteger ab = Orient(a, b, point);
            BigInteger bc = Orient(b, c, point);
            BigInteger ca = Orient(c, a, point);
            return ab > BigInteger.Zero && bc > BigInteger.Zero && ca > BigInteger.Zero;
        }

        private static Node FindRemovableCollinear(Node ring, List<LVector3> vertices)
        {
            Node current = ring;
            do
            {
                if (current.previous.vertex != current.next.vertex &&
                    Orient(
                        vertices[current.previous.vertex],
                        vertices[current.vertex],
                        vertices[current.next.vertex]) == BigInteger.Zero)
                    return current;
                current = current.next;
            } while (!ReferenceEquals(current, ring));
            return null;
        }

        private static int CountNodes(Node ring)
        {
            int count = 0;
            Node current = ring;
            do
            {
                count++;
                current = current.next;
                if (count > 1_000_000) return 0;
            } while (!ReferenceEquals(current, ring));
            return count;
        }

        private static void RemoveNode(Node node)
        {
            node.previous.next = node.next;
            node.next.previous = node.previous;
        }

        private static void AddCounterClockwiseTriangle(
            List<IndexTriangle> triangles,
            int a,
            int b,
            int c,
            List<LVector3> vertices)
        {
            BigInteger orientation = Orient(vertices[a], vertices[b], vertices[c]);
            if (orientation > BigInteger.Zero)
                triangles.Add(new IndexTriangle(a, b, c));
            else if (orientation < BigInteger.Zero)
                triangles.Add(new IndexTriangle(a, c, b));
        }

        private static void Legalize(
            List<LVector3> vertices,
            List<IndexTriangle> triangles,
            HashSet<IndexEdge> constraints)
        {
            long calculatedLimit = (long)triangles.Count * triangles.Count * 4L;
            int flipLimit = (int)Math.Min(Math.Max(calculatedLimit, 32L), int.MaxValue);
            for (int iteration = 0; iteration < flipLimit; iteration++)
            {
                Dictionary<IndexEdge, EdgeOwners> owners = BuildEdgeOwners(triangles);
                var orderedEdges = new List<IndexEdge>(owners.Keys);
                orderedEdges.Sort(CompareIndexEdges);
                bool flipped = false;
                for (int edgeIndex = 0; edgeIndex < orderedEdges.Count; edgeIndex++)
                {
                    IndexEdge edge = orderedEdges[edgeIndex];
                    EdgeOwners edgeOwners = owners[edge];
                    if (edgeOwners.second < 0 || constraints.Contains(edge)) continue;

                    IndexTriangle first = triangles[edgeOwners.first];
                    IndexTriangle second = triangles[edgeOwners.second];
                    int oppositeFirst = GetOpposite(first, edge);
                    int oppositeSecond = GetOpposite(second, edge);
                    if (oppositeFirst < 0 || oppositeSecond < 0 || oppositeFirst == oppositeSecond)
                        continue;

                    LVector3 a = vertices[edge.a];
                    LVector3 b = vertices[edge.b];
                    LVector3 c = vertices[oppositeFirst];
                    LVector3 d = vertices[oppositeSecond];
                    if (!IsConvexQuadrilateral(a, b, c, d) ||
                        !IsInsideCircumcircle(a, b, c, d))
                        continue;

                    triangles[edgeOwners.first] = MakeCounterClockwise(
                        oppositeFirst,
                        oppositeSecond,
                        edge.a,
                        vertices);
                    triangles[edgeOwners.second] = MakeCounterClockwise(
                        oppositeSecond,
                        oppositeFirst,
                        edge.b,
                        vertices);
                    flipped = true;
                    break;
                }

                if (!flipped) return;
            }
        }

        private static int CompareIndexEdges(IndexEdge left, IndexEdge right)
        {
            int result = left.a.CompareTo(right.a);
            return result != 0 ? result : left.b.CompareTo(right.b);
        }

        private static Dictionary<IndexEdge, EdgeOwners> BuildEdgeOwners(
            List<IndexTriangle> triangles)
        {
            var result = new Dictionary<IndexEdge, EdgeOwners>();
            for (int i = 0; i < triangles.Count; i++)
            {
                IndexTriangle triangle = triangles[i];
                AddOwner(result, new IndexEdge(triangle.a, triangle.b), i);
                AddOwner(result, new IndexEdge(triangle.b, triangle.c), i);
                AddOwner(result, new IndexEdge(triangle.c, triangle.a), i);
            }
            return result;
        }

        private static void AddOwner(
            Dictionary<IndexEdge, EdgeOwners> owners,
            IndexEdge edge,
            int triangle)
        {
            if (owners.TryGetValue(edge, out EdgeOwners current))
                owners[edge] = new EdgeOwners(current.first, triangle);
            else
                owners.Add(edge, new EdgeOwners(triangle));
        }

        private static int GetOpposite(IndexTriangle triangle, IndexEdge edge)
        {
            if (triangle.a != edge.a && triangle.a != edge.b) return triangle.a;
            if (triangle.b != edge.a && triangle.b != edge.b) return triangle.b;
            if (triangle.c != edge.a && triangle.c != edge.b) return triangle.c;
            return -1;
        }

        private static bool IsConvexQuadrilateral(
            LVector3 a,
            LVector3 b,
            LVector3 c,
            LVector3 d)
        {
            BigInteger sideC = Orient(a, b, c);
            BigInteger sideD = Orient(a, b, d);
            BigInteger sideA = Orient(c, d, a);
            BigInteger sideB = Orient(c, d, b);
            return sideC != BigInteger.Zero &&
                   sideD != BigInteger.Zero &&
                   sideA != BigInteger.Zero &&
                   sideB != BigInteger.Zero &&
                   (sideC > 0) != (sideD > 0) &&
                   (sideA > 0) != (sideB > 0);
        }

        private static bool IsInsideCircumcircle(
            LVector3 a,
            LVector3 b,
            LVector3 c,
            LVector3 point)
        {
            BigInteger ax = (BigInteger)a._x - point._x;
            BigInteger ay = (BigInteger)a._z - point._z;
            BigInteger bx = (BigInteger)b._x - point._x;
            BigInteger by = (BigInteger)b._z - point._z;
            BigInteger cx = (BigInteger)c._x - point._x;
            BigInteger cy = (BigInteger)c._z - point._z;
            BigInteger determinant =
                (ax * ax + ay * ay) * (bx * cy - by * cx) -
                (bx * bx + by * by) * (ax * cy - ay * cx) +
                (cx * cx + cy * cy) * (ax * by - ay * bx);
            BigInteger orientation = Orient(a, b, c);
            return orientation > BigInteger.Zero
                ? determinant > BigInteger.Zero
                : determinant < BigInteger.Zero;
        }

        private static IndexTriangle MakeCounterClockwise(
            int a,
            int b,
            int c,
            List<LVector3> vertices)
        {
            return Orient(vertices[a], vertices[b], vertices[c]) > BigInteger.Zero
                ? new IndexTriangle(a, b, c)
                : new IndexTriangle(a, c, b);
        }

        private static BigInteger SignedArea(List<LVector3> points)
        {
            BigInteger result = BigInteger.Zero;
            for (int i = 0; i < points.Count; i++)
            {
                LVector3 current = points[i];
                LVector3 next = points[(i + 1) % points.Count];
                result += (BigInteger)current._x * next._z - (BigInteger)next._x * current._z;
            }
            return result;
        }

        private static BigInteger Orient(LVector3 a, LVector3 b, LVector3 c)
        {
            return ((BigInteger)b._x - a._x) * ((BigInteger)c._z - a._z) -
                   ((BigInteger)b._z - a._z) * ((BigInteger)c._x - a._x);
        }
    }
}
