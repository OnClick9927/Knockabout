namespace Lockstep.Nav
{
    /// <summary>
    /// 导航几何的容差比较、三角形包含、共享边、平面吸附与启发距离工具。
    /// 所有点包含判断基于 XZ 平面，Y 由三角形平面方程重新计算。
    /// </summary>
    public static class NavHelper
    {
        public static readonly LFloat epsilon = LFloat.FromRaw(100L); // 0.0001

        public static bool SamePoint(LVector3 a, LVector3 b)
        {
            return LMath.IsSame(a, b, epsilon);
        }

        /// <summary>通过允许 epsilon 负误差的重心坐标判断点是否落在三角形 XZ 投影内。</summary>
        public static bool ContainsPointXZ(this Triangle triangle, LVector3 point)
        {
            if (!triangle.TryGetXZBarycentric(point, out LFloat u, out LFloat v, out LFloat w))
                return false;

            return u >= -epsilon && v >= -epsilon && w >= -epsilon;
        }

        public static bool GetSharedEdge(this Triangle triangle, Triangle other, out Edge sharedEdge)
        {
            sharedEdge = default;
            var edges = triangle.edges;
            for (int i = 0; i < edges.Length; i++)
            {
                for (int j = 0; j < other.edges.Length; j++)
                {
                    if (TryGetOverlappingEdge(edges[i], other.edges[j], out sharedEdge))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 求两条三维共线线段的重叠部分。
        /// 简化后的大矩形边可能与相邻小矩形形成 T 形连接，此时两边端点不完全相同，
        /// 但重叠部分仍是合法的导航 Portal。
        /// </summary>
        internal static bool TryGetOverlappingEdge(Edge first, Edge second, out Edge overlap)
        {
            overlap = default;
            LVector3 direction = first.b - first.a;
            LFloat lengthSquared = LVector3.Dot(direction, direction);
            if (lengthSquared <= LFloat.EPSILON) return false;

            LFloat length = LMath.Sqrt(lengthSquared);
            LFloat lineTolerance = epsilon * length;
            if (LVector3.Cross(direction, second.a - first.a).magnitude > lineTolerance ||
                LVector3.Cross(direction, second.b - first.a).magnitude > lineTolerance)
                return false;

            LFloat secondA = LVector3.Dot(second.a - first.a, direction) / lengthSquared;
            LFloat secondB = LVector3.Dot(second.b - first.a, direction) / lengthSquared;
            LFloat overlapStart = LMath.Max(
                LFloat.zero,
                LMath.Min(secondA, secondB));
            LFloat overlapEnd = LMath.Min(
                LFloat.one,
                LMath.Max(secondA, secondB));
            if (overlapEnd - overlapStart <= epsilon) return false;

            overlap = Edge.Create(
                first.a + direction * overlapStart,
                first.a + direction * overlapEnd);
            return true;
        }

        /// <summary>保持 XZ 不变，把点的 Y 投影到三角形平面。</summary>
        public static LVector3 Snap(this Triangle triangle, LVector3 point)
        {
            LVector3 normal = triangle.GetPlaneNormal();
            LFloat y = triangle.point1.y;
            if (LMath.Abs(normal.y) > LFloat.EPSILON)
            {
                LVector3 offset = point - triangle.point1;
                y -= (normal.x * offset.x + normal.z * offset.z) / normal.y;
            }
            return new LVector3(point.x, y, point.z);
        }

        /// <summary>
        /// 取得稳定的三角形内部代表点，优先使用按对边长度加权的候选，退化时回退到重心或顶点。
        /// </summary>
        internal static LVector3 GetInteriorPoint(this Triangle triangle)
        {
            LFloat point1Weight = Heuristic(triangle.point2, triangle.point3);
            LFloat point2Weight = Heuristic(triangle.point3, triangle.point1);
            LFloat point3Weight = Heuristic(triangle.point1, triangle.point2);
            LFloat weightSum = point1Weight + point2Weight + point3Weight;
            if (weightSum <= epsilon)
                return triangle.point1;

            LVector3 candidate = new LVector3(
                (triangle.point1.x * point1Weight +
                 triangle.point2.x * point2Weight +
                 triangle.point3.x * point3Weight) / weightSum,
                (triangle.point1.y * point1Weight +
                 triangle.point2.y * point2Weight +
                 triangle.point3.y * point3Weight) / weightSum,
                (triangle.point1.z * point1Weight +
                 triangle.point2.z * point2Weight +
                 triangle.point3.z * point3Weight) / weightSum);
            if (triangle.ContainsPointXZ(candidate))
                return triangle.Snap(candidate);

            LVector3 centroid = LVector3.Average(
                triangle.point1, triangle.point2, triangle.point3);
            return triangle.ContainsPointXZ(centroid)
                ? triangle.Snap(centroid)
                : triangle.point1;
        }

        internal static LFloat Heuristic(LVector3 a, LVector3 b) => (a - b).magnitude;
    }
}
