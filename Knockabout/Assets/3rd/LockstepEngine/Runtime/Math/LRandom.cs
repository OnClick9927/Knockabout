// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using System;

namespace Lockstep {
    /// <summary>
    /// 可序列化、可复现的确定性伪随机数生成器。
    /// 相同种子和相同调用顺序必然得到相同结果，适合锁步模拟；
    /// 它不具备密码学安全性，也不应与 UnityEngine.Random 混用。
    /// </summary>
    public partial struct Random {
        /// <summary>当前随机状态；回滚快照必须一并保存和恢复。</summary>
        public ulong randSeed ;
        public Random(uint seed = 17){
            randSeed = seed;
        }
        public LFloat value =>  LFloat.FromRaw(  Range(0, (int)LFloat.Precision));

        /// <summary>推进一次线性同余序列并返回新的无符号随机值。</summary>
        public uint Next(){
            randSeed = randSeed * 1103515245 + 36153;
            return (uint) (randSeed / 65536);
        }

        private ulong NextUInt64(ulong max){
            if (max <= uint.MaxValue)
                return Next((uint)max);

            ulong value = ((ulong)Next() << 32) | Next();
            return value % max;
        }

        // 返回半开区间 [0, max)，max 为 0 没有合法结果。
        public uint Next(uint max){
            if (max == 0)
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than zero.");
            return Next() % max;
        }
        public LVector2 NextVector2(){
            return LVector2.CreateFromRaw(Next((uint)LFloat.Precision), Next((uint)LFloat.Precision));
        }
        public LVector3 NextVector3(){
            return LVector3.CreateFromRaw(
                Next((uint)LFloat.Precision),
                Next((uint)LFloat.Precision),
                Next((uint)LFloat.Precision));
        }
        public int Next(int max){
            if (max <= 0)
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than zero.");
            return (int)(Next() % (uint)max);
        }
        // Range 重载统一采用半开区间 [min, max)，min == max 时直接返回端点。
        public uint Range(uint min, uint max){
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min),
                    string.Format("'{0}' cannot be greater than {1}.", min, max));
            if (min == max)
                return min;

            uint num = max - min;
            return this.Next(num) + min;
        }
        public int Range(int min, int max){
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min),
                    string.Format("'{0}' cannot be greater than {1}.", min, max));
            if (min == max)
                return min;

            uint num = (uint)((long)max - min);
            return (int)(this.Next(num) + (long)min);
        }

        public LFloat Range(LFloat min, LFloat max){
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min),
                    string.Format("'{0}' cannot be greater than {1}.", min, max));
            if (min == max)
                return min;

            ulong num = unchecked((ulong)max._val - (ulong)min._val);
            ulong offset = NextUInt64(num);
            return LFloat.FromRaw(unchecked(min._val + (long)offset));
        }
        public override string ToString()
        {
            return $"{nameof(randSeed)}_{randSeed}";
        }
    }

}
