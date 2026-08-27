using System;

namespace Lockstep
{
    /// <summary>
    /// Lockstep 通用的三维轴对齐包围盒，使用闭区间 min/max。
    /// <para>
    /// 导航、三维碰撞和宽相位树统一使用此类型，避免同一份 AABB 数据在模块之间重复定义和转换。
    /// SetMinMax 会自动整理每个轴的大小关系；Contains 可传入误差向量，
    /// 用于容忍定点量化后落在三角形或 BVH 边界附近的点。
    /// </para>
    /// </summary>
    [Serializable]
    public struct LBounds
    {
        public LVector3 min;
        public LVector3 max;
        public LVector3 center => LVector3.Average(min, max);
        public LVector3 size => max - min;
        public LVector3 extents => size / 2;

        public static LBounds zero => new LBounds(LVector3.zero, LVector3.zero);

        /// <summary>使用两个端点创建包围盒；输入顺序可以颠倒，构造函数会逐轴整理。</summary>
        public LBounds(LVector3 min, LVector3 max)
        {
            this.min = LVector3.zero;
            this.max = LVector3.zero;
            SetMinMax(min, max);
        }

        /// <summary>使用中心和半尺寸创建包围盒；负半尺寸会按绝对值处理。</summary>
        public static LBounds Create(LVector3 center, LVector3 extents)
        {
            extents = Abs(extents);
            return new LBounds(center - extents, center + extents);
        }

        public void SetMinMax(LVector3 min, LVector3 max)
        {
            this.min = new LVector3(
                LMath.Min(min.x, max.x),
                LMath.Min(min.y, max.y),
                LMath.Min(min.z, max.z));
            this.max = new LVector3(
                LMath.Max(min.x, max.x),
                LMath.Max(min.y, max.y),
                LMath.Max(min.z, max.z));
        }

        public void Encapsulate(LBounds bounds)
        {
            Encapsulate(bounds.min);
            Encapsulate(bounds.max);
        }

        /// <summary>扩张包围盒，使指定点落入新的闭区间内。</summary>
        public void Encapsulate(LVector3 point)
        {
            min = new LVector3(
                LMath.Min(min.x, point.x),
                LMath.Min(min.y, point.y),
                LMath.Min(min.z, point.z));
            max = new LVector3(
                LMath.Max(max.x, point.x),
                LMath.Max(max.y, point.y),
                LMath.Max(max.z, point.z));
        }

        /// <summary>判断点是否位于包围盒闭区间内，落在任意边界面上也视为包含。</summary>
        public bool Contains(LVector3 point)
        {
            return point.x >= min.x && point.x <= max.x
                && point.y >= min.y && point.y <= max.y
                && point.z >= min.z && point.z <= max.z;
        }

        /// <summary>判断另一个包围盒是否完整位于当前包围盒内。</summary>
        public bool Contains(LBounds bounds)
        {
            return Contains(bounds.min) && Contains(bounds.max);
        }

        /// <summary>按每个轴独立扩张误差后，判断点是否位于包围盒内。</summary>
        public bool Contains(LVector3 point, LVector3 eps)
        {
            return point.x >= min.x - eps.x && point.x <= max.x + eps.x
                && point.y >= min.y - eps.y && point.y <= max.y + eps.y
                && point.z >= min.z - eps.z && point.z <= max.z + eps.z;
        }

        /// <summary>判断两个闭区间包围盒是否重叠；仅在边界接触时同样返回 true。</summary>
        public bool Overlaps(LBounds bounds)
        {
            return min.x <= bounds.max.x && max.x >= bounds.min.x
                && min.y <= bounds.max.y && max.y >= bounds.min.y
                && min.z <= bounds.max.z && max.z >= bounds.min.z;
        }

        /// <summary>
        /// 返回点到包围盒的最短距离平方。点位于盒内时每个轴的距离均为零，结果也为零。
        /// 使用平方距离可避免宽相位查询中不必要的开方运算。
        /// </summary>
        public LFloat DistanceSquared(LVector3 point)
        {
            LFloat dx = point.x < min.x
                ? min.x - point.x
                : point.x > max.x ? point.x - max.x : LFloat.zero;
            LFloat dy = point.y < min.y
                ? min.y - point.y
                : point.y > max.y ? point.y - max.y : LFloat.zero;
            LFloat dz = point.z < min.z
                ? min.z - point.z
                : point.z > max.z ? point.z - max.z : LFloat.zero;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>逐轴取绝对值，保证 Create 接收到的半尺寸不会反转边界。</summary>
        private static LVector3 Abs(LVector3 value)
        {
            return new LVector3(
                LMath.Abs(value.x),
                LMath.Abs(value.y),
                LMath.Abs(value.z));
        }
    }
}
