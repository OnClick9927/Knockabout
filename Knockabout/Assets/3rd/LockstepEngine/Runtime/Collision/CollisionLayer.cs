using System;

namespace Lockstep.Collision
{
    /// <summary>
    /// 碰撞查询层的轻量值类型。层只保存稳定整数编号；查询传入空层数组表示匹配全部层。
    /// </summary>
    public struct CollisionLayer : IEquatable<CollisionLayer>
    {
        public static CollisionLayer Default = Get(0);
        public static CollisionLayer Get(int value) => new CollisionLayer { value = value };


        /// <summary>树内部用于过滤和排序的层编号。</summary>
        internal int value { get; set; }


        public static bool operator !=(CollisionLayer left, CollisionLayer right)
        {
            return !(left == right);
        }
        public static bool operator ==(CollisionLayer a, CollisionLayer b)
        {
            return a.value == b.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is CollisionLayer other && Equals(other);
        }

        public bool Equals(CollisionLayer other) => value == other.value;
    }

}
