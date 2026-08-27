// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 


namespace Lockstep
{
    /// <summary>
    /// 三维正交坐标基。WorldToLocal 通过与基向量点积得到局部坐标，
    /// LocalToWorld 则按三个基向量线性组合还原世界向量。
    /// 调用方应保证 x/y/z 已归一化且互相正交。
    /// </summary>
    public struct LAxis3D
    {
        public LVector3 x;
        public LVector3 y;
        public LVector3 z;
        public static readonly LAxis3D identity = new LAxis3D(LVector3.right, LVector3.up, LVector3.forward);

        public LAxis3D(LVector3 right, LVector3 up, LVector3 forward)
        {
            this.x = right;
            this.y = up;
            this.z = forward;
        }

        /// <summary>把世界空间向量投影到当前局部坐标基。</summary>
        public LVector3 WorldToLocal(LVector3 vec)
        {
            var _x = LMath.Dot(x, vec);
            var _y = LMath.Dot(y, vec);
            var _z = LMath.Dot(z, vec);
            return new LVector3(_x, _y, _z);
        }
        /// <summary>把当前坐标基中的局部向量转换回世界空间。</summary>
        public LVector3 LocalToWorld(LVector3 vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }

        public LVector3 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: throw new System.IndexOutOfRangeException("vector idx invalid" + index);
                }
            }

            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default: throw new System.IndexOutOfRangeException("vector idx invalid" + index);
                }
            }
        }
    }
}
