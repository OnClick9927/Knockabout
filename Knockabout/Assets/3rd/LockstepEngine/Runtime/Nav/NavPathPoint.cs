using System;

namespace Lockstep.Nav
{
    /// <summary>
    /// 最终路径中的带类型拐点。普通漏斗拐点为 Point，路径首尾为 Start/End，
    /// 跨越非连续 TriangleLink 时用 LinkFrom/LinkTo 明确业务切换位置。
    /// </summary>
    [Serializable]
    public struct NavPathPoint
    {
        /// <summary>路径点在移动流程中的语义。</summary>
        public enum PointType
        {
            Point,
            Start, End,
            LinkFrom,
            LinkTo
        }

        public PointType type;
        public LVector3 position;

        public NavPathPoint(PointType type, LVector3 position)
        {
            this.type = type;
            this.position = position;
        }
    }
}
