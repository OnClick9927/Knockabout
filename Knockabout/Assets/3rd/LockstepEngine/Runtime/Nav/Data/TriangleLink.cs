using System;

namespace Lockstep.Nav
{
    /// <summary>
    /// 三角形到相邻三角形的有向连接。
    /// <para><see cref="from"/> 与 <see cref="to"/> 构成两三角形共享的门边，
    /// 寻路展开时会按当前行进方向把它解释为漏斗算法的左右端点。</para>
    /// </summary>
    [Serializable]
    public class TriangleLink
    {
        public int neighbor;        // 目标三角形索引
        public LFloat cost;         // 穿越该链接的移动代价（定点数）
        /// <summary>共享门边的起点。</summary>
        public LVector3 from;
        /// <summary>共享门边的终点。</summary>
        public LVector3 to;
    }
}
