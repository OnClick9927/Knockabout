using System;
using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// 一个导航三角形及其图连接。
    /// points/edges/bounds 是几何数据，neighbors 是共享边邻接索引，links 是非连续跳转。
    /// 非序列化缓存保存平面法线和 XZ 重心坐标系系数，顶点变化时自动失效重建。
    /// </summary>
    [Serializable]
    public class Triangle
    {
        public LVector3[] points = new LVector3[3];
        public Edge[] edges = new Edge[3];
        public LBounds bounds;
        public List<int> neighbors = new List<int>();
        public List<TriangleLink> links = new List<TriangleLink>();
        public LVector3 point1 => points[0];
        public LVector3 point2 => points[1];
        public LVector3 point3 => points[2];

        [NonSerialized] private bool geometryCacheValid;
        [NonSerialized] private LVector3 cachedPoint1;
        [NonSerialized] private LVector3 cachedPoint2;
        [NonSerialized] private LVector3 cachedPoint3;
        [NonSerialized] private LVector3 cachedPlaneNormal;
        [NonSerialized] private LFloat cachedXZDenominator;
        [NonSerialized] private LFloat cachedBzMinusCz;
        [NonSerialized] private LFloat cachedCxMinusBx;
        [NonSerialized] private LFloat cachedCzMinusAz;
        [NonSerialized] private LFloat cachedAxMinusCx;

        /// <summary>在顶点首次使用或发生变化后重建平面与重心坐标缓存。</summary>
        private void EnsureGeometryCache()
        {
            if (geometryCacheValid &&
                cachedPoint1 == point1 &&
                cachedPoint2 == point2 &&
                cachedPoint3 == point3)
                return;

            cachedPoint1 = point1;
            cachedPoint2 = point2;
            cachedPoint3 = point3;
            cachedPlaneNormal = LVector3.Cross(point2 - point1, point3 - point1);
            cachedBzMinusCz = point2.z - point3.z;
            cachedCxMinusBx = point3.x - point2.x;
            cachedCzMinusAz = point3.z - point1.z;
            cachedAxMinusCx = point1.x - point3.x;
            cachedXZDenominator =
                cachedBzMinusCz * cachedAxMinusCx +
                cachedCxMinusBx * (point1.z - point3.z);
            geometryCacheValid = true;
        }

        internal LVector3 GetPlaneNormal()
        {
            EnsureGeometryCache();
            return cachedPlaneNormal;
        }

        /// <summary>在 XZ 投影上计算重心坐标；投影退化为线段时返回 false。</summary>
        internal bool TryGetXZBarycentric(LVector3 point, out LFloat u, out LFloat v, out LFloat w)
        {
            EnsureGeometryCache();
            if (LMath.Abs(cachedXZDenominator) <= LFloat.EPSILON)
            {
                u = LFloat.zero;
                v = LFloat.zero;
                w = LFloat.zero;
                return false;
            }

            u =
                (cachedBzMinusCz * (point.x - point3.x) +
                 cachedCxMinusBx * (point.z - point3.z)) / cachedXZDenominator;
            v =
                (cachedCzMinusAz * (point.x - point3.x) +
                 cachedAxMinusCx * (point.z - point3.z)) / cachedXZDenominator;
            w = LFloat.one - u - v;
            return true;
        }
    }
}
