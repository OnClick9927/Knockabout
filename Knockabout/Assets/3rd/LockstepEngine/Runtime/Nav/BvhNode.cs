using System;
using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// 导航三角形的静态包围盒层次树。
    /// 构建时按当前范围最长轴用 quickselect 中位切分；查询时先按 XZ 范围和 Y 高差剪枝，
    /// 最终选择包含投影且高度最近的三角形，并用几何和原始序号稳定打破平局。
    /// </summary>
    class BvhNode
    {
        private sealed class AxisComparer : IComparer<BvhNode>
        {
            private readonly int axis;

            public AxisComparer(int axis)
            {
                this.axis = axis;
            }

            public int Compare(BvhNode a, BvhNode b)
            {
                int result = a.center[axis].CompareTo(b.center[axis]);
                if (result != 0) return result;
                result = CompareTriangles(a.triangle, b.triangle);
                if (result != 0) return result;
                return a.order.CompareTo(b.order);
            }
        }

        private static readonly IComparer<BvhNode>[] axisComparers =
        {
            new AxisComparer(0),
            new AxisComparer(1),
            new AxisComparer(2)
        };

        private LBounds bounds;
        private LVector3 center;
        private BvhNode leftChild;
        private BvhNode rightChild;
        private Triangle triangle;
        private int order;

        private static int CompareTriangles(Triangle a, Triangle b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            for (int i = 0; i < 3; i++)
            {
                int result = a.points[i]._x.CompareTo(b.points[i]._x);
                if (result != 0) return result;
                result = a.points[i]._y.CompareTo(b.points[i]._y);
                if (result != 0) return result;
                result = a.points[i]._z.CompareTo(b.points[i]._z);
                if (result != 0) return result;
            }
            return 0;
        }

        /// <summary>递归合并范围，并沿最长轴把叶节点近似均分。</summary>
        private static BvhNode BuildNodeRecursive(BvhNode[] nodes, int start, int count)
        {
            if (count == 1) return nodes[start];

            LBounds merged = nodes[start].bounds;
            for (int i = start + 1; i < start + count; i++)
                merged.Encapsulate(nodes[i].bounds);

            LVector3 size = merged.size;
            int axis = (size.x >= size.y && size.x >= size.z) ? 0 :
                       (size.y >= size.x && size.y >= size.z) ? 1 : 2;
            int leftCount = count / 2;
            Select(nodes, start, start + count - 1, start + leftCount, axisComparers[axis]);
            BvhNode left = BuildNodeRecursive(nodes, start, leftCount);
            BvhNode right = BuildNodeRecursive(nodes, start + leftCount, count - leftCount);
            return new BvhNode
            {
                bounds = merged,
                leftChild = left,
                rightChild = right
            };
        }

        /// <summary>原地 quickselect，只保证目标中位位置正确，无需完整排序。</summary>
        private static void Select(
            BvhNode[] nodes,
            int left,
            int right,
            int target,
            IComparer<BvhNode> comparer)
        {
            while (left < right)
            {
                int pivotIndex = left + ((right - left) >> 1);
                BvhNode pivot = nodes[pivotIndex];
                Swap(nodes, pivotIndex, right);

                int storeIndex = left;
                for (int i = left; i < right; i++)
                {
                    if (comparer.Compare(nodes[i], pivot) < 0)
                    {
                        Swap(nodes, storeIndex, i);
                        storeIndex++;
                    }
                }
                Swap(nodes, storeIndex, right);

                if (storeIndex == target) return;
                if (target < storeIndex)
                    right = storeIndex - 1;
                else
                    left = storeIndex + 1;
            }
        }

        private static void Swap(BvhNode[] nodes, int a, int b)
        {
            if (a == b) return;
            BvhNode value = nodes[a];
            nodes[a] = nodes[b];
            nodes[b] = value;
        }

        /// <summary>为全部三角形建立不可变 BVH；空输入返回 null。</summary>
        public static BvhNode Build(List<Triangle> triangles)
        {
            if (triangles == null || triangles.Count == 0) return null;

            BvhNode[] leafNodes = new BvhNode[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle triangle = triangles[i];
                leafNodes[i] = new BvhNode
                {
                    bounds = triangle.bounds,
                    center = triangle.bounds.center,
                    triangle = triangle,
                    order = i
                };
            }

            return BuildNodeRecursive(leafNodes, 0, leafNodes.Length);
        }

        private static LFloat GetNodeDistance(BvhNode node, LVector3 point, LVector3 boundEps)
        {
            if (node == null ||
                point.x < node.bounds.min.x - boundEps.x ||
                point.x > node.bounds.max.x + boundEps.x ||
                point.z < node.bounds.min.z - boundEps.z ||
                point.z > node.bounds.max.z + boundEps.z)
                return LFloat.MaxValue;

            LFloat minY = node.bounds.min.y - boundEps.y;
            LFloat maxY = node.bounds.max.y + boundEps.y;
            if (point.y < minY) return minY - point.y;
            if (point.y > maxY) return point.y - maxY;
            return LFloat.zero;
        }

        private static void QueryNearestNode(
            BvhNode node,
            LFloat nodeDistance,
            LVector3 point,
            LVector3 boundEps,
            ref Triangle closestTriangle,
            ref LVector3 closestPoint,
            ref LFloat closestHeightDistance,
            ref int closestOrder)
        {
            if (node == null || nodeDistance > closestHeightDistance)
                return;

            if (node.triangle != null)
            {
                if (!node.triangle.ContainsPointXZ(point))
                    return;

                LVector3 candidatePoint = node.triangle.Snap(point);
                LFloat heightDistance = LMath.Abs(candidatePoint.y - point.y);
                if (closestTriangle != null)
                {
                    if (heightDistance > closestHeightDistance)
                        return;
                    if (heightDistance == closestHeightDistance)
                    {
                        int comparison = CompareTriangles(node.triangle, closestTriangle);
                        if (comparison > 0 || comparison == 0 && node.order >= closestOrder)
                            return;
                    }
                }

                closestTriangle = node.triangle;
                closestPoint = candidatePoint;
                closestHeightDistance = heightDistance;
                closestOrder = node.order;
                return;
            }

            LFloat leftDistance = GetNodeDistance(node.leftChild, point, boundEps);
            LFloat rightDistance = GetNodeDistance(node.rightChild, point, boundEps);
            if (rightDistance < leftDistance)
            {
                QueryNearestNode(
                    node.rightChild, rightDistance, point, boundEps,
                    ref closestTriangle, ref closestPoint, ref closestHeightDistance, ref closestOrder);
                QueryNearestNode(
                    node.leftChild, leftDistance, point, boundEps,
                    ref closestTriangle, ref closestPoint, ref closestHeightDistance, ref closestOrder);
            }
            else
            {
                QueryNearestNode(
                    node.leftChild, leftDistance, point, boundEps,
                    ref closestTriangle, ref closestPoint, ref closestHeightDistance, ref closestOrder);
                QueryNearestNode(
                    node.rightChild, rightDistance, point, boundEps,
                    ref closestTriangle, ref closestPoint, ref closestHeightDistance, ref closestOrder);
            }
        }

        /// <summary>查找 XZ 包含给定点且垂直距离最近的三角形，并返回吸附点。</summary>
        public bool TryGetTriangle(
            LVector3 point,
            LVector3 boundEps,
            out Triangle triangle,
            out LVector3 snappedPoint)
        {
            triangle = null;
            snappedPoint = point;
            LFloat closestHeightDistance = LFloat.MaxValue;
            int closestOrder = int.MaxValue;
            QueryNearestNode(
                this,
                GetNodeDistance(this, point, boundEps),
                point,
                boundEps,
                ref triangle,
                ref snappedPoint,
                ref closestHeightDistance,
                ref closestOrder);
            return triangle != null;
        }
    }
}
