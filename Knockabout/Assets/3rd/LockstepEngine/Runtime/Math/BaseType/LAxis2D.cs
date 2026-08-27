// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

namespace Lockstep
{
    /// <summary>
    /// 二维局部坐标轴，由互相垂直的 x、y 基向量组成。
    /// 索引 0/1 分别访问 x/y，用于需要按轴循环处理的几何算法。
    /// </summary>
    public struct LAxis2D
    {
        public LVector3 x;
        public LVector3 y;
        
        public static readonly LAxis2D identity = new LAxis2D(LVector3.right, LVector3.up);
        public LAxis2D(LVector3 x, LVector3 y)
        {
            this.x = x;
            this.y = y;
        }
        public LVector3 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
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
                    default: throw new System.IndexOutOfRangeException("vector idx invalid" + index);
                }
            }
        }
    }


  
}
