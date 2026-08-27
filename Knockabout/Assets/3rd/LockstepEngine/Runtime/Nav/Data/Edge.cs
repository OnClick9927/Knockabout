using System;

namespace Lockstep.Nav
{
    /// <summary>
    /// 导航三角形的一条无向边。相等比较允许端点顺序相反，并使用 SamePoint 容差。
    /// 因容差相等不满足传递性，GetHashCode 固定返回 0，不能作为高性能哈希键。
    /// </summary>
    [Serializable]
    public struct Edge
    {
        public LVector3 a;
        public LVector3 b;

        public static Edge Create(LVector3 a, LVector3 b)
        {
            return new Edge
            {
                a = a,
                b = b
            };
        }

        public static bool operator ==(Edge lhs, Edge rhs)
        {
            return (NavHelper.SamePoint(lhs.a, rhs.a) && NavHelper.SamePoint(lhs.b, rhs.b)) ||
                   (NavHelper.SamePoint(lhs.a, rhs.b) && NavHelper.SamePoint(lhs.b, rhs.a));
        }

        public static bool operator !=(Edge lhs, Edge rhs) => !(lhs == rhs);

        public override bool Equals(object obj) => obj is Edge && this == (Edge)obj;

        // SamePoint 使用容差且不具传递性，因此不存在既有用又满足相等契约的坐标哈希。
        public override int GetHashCode() => 0;
    }
}
