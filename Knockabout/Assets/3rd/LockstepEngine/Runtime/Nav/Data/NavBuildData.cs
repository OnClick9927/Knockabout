using System;
using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// 与 Unity 无关的导航网格生成参数。
    /// 所有长度使用世界单位的定点数；修改参数后，同一输入三角形集合会得到确定性的输出。
    /// </summary>
    [Serializable]
    public class NavBuildSettings
    {
        /// <summary>高度场单元格在 XZ 平面上的边长。越小越精确，但内存和生成时间按平方增长。</summary>
        public LFloat cellSize = LFloat.FromRaw(250000L);

        /// <summary>代理碰撞半径。生成器会从不可行走边界向内腐蚀对应距离。</summary>
        public LFloat agentRadius = LFloat.FromRaw(500000L);

        /// <summary>代理站立所需的最小垂直净空。</summary>
        public LFloat agentHeight = LFloat.FromRaw(2000000L);

        /// <summary>相邻高度场单元之间允许直接行走的最大高度差。</summary>
        public LFloat maxStepHeight = LFloat.FromRaw(500000L);

        /// <summary>允许行走表面与世界向上方向的最小点积，即最大坡度余弦。</summary>
        public LFloat minWalkableNormalY = LFloat.FromRaw(707107L);

        /// <summary>少于该单元数量的孤立连通区域会被移除；0 或 1 表示保留所有区域。</summary>
        public int minRegionCells = 1;

        /// <summary>
        /// 是否启用共面区域简化。开启约束德洛内时优先按轮廓剖分，退化区域回退到矩形合并；
        /// 关闭时每个高度场单元固定输出两个三角形，主要用于生成结果诊断。
        /// </summary>
        public bool mergeCoplanarCells = true;

        /// <summary>
        /// 是否在共面区域提取简化轮廓，并使用约束德洛内三角剖分代替矩形切分。
        /// 轮廓边和孔洞边始终作为不可翻转约束；退化轮廓会自动回退到矩形合并。
        /// </summary>
        public bool useConstrainedDelaunay = true;

        /// <summary>输出数据的业务代理类型编号，不再关联 Unity NavMesh agentTypeID。</summary>
        public int agentType;

        /// <summary>验证并归一化参数，防止除零或负尺寸进入栅格化流程。</summary>
        internal void Validate()
        {
            if (cellSize <= LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be greater than zero.");

            agentRadius = LMath.Max(agentRadius, LFloat.zero);
            agentHeight = LMath.Max(agentHeight, LFloat.zero);
            maxStepHeight = LMath.Max(maxStepHeight, LFloat.zero);
            minWalkableNormalY = LMath.Clamp(minWalkableNormalY, LFloat.zero, LFloat.one);
            minRegionCells = LMath.Max(minRegionCells, 1);
        }
    }

    /// <summary>导航烘焙输入中的一个世界空间三角形。</summary>
    [Serializable]
    public struct NavBuildTriangle
    {
        public LVector3 a;
        public LVector3 b;
        public LVector3 c;

        /// <summary>
        /// 为 true 时，该三角形仍作为实体参与净空和障碍计算，但不会生成可行走高度场样本。
        /// 该字段默认 false，普通调用方只传三个顶点即可保持原有行为。
        /// </summary>
        public bool blockWalkableSurface;

        public NavBuildTriangle(
            LVector3 a,
            LVector3 b,
            LVector3 c,
            bool blockWalkableSurface = false)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.blockWalkableSurface = blockWalkableSurface;
        }
    }

    /// <summary>
    /// 不连续导航区域之间的离线跳转描述。
    /// 端点会吸附到生成后的三角形；双向链接会自动生成反向记录。
    /// </summary>
    [Serializable]
    public struct NavBuildLink
    {
        public LVector3 from;
        public LVector3 to;
        public LFloat cost;
        public bool bidirectional;

        public NavBuildLink(LVector3 from, LVector3 to, LFloat cost, bool bidirectional = true)
        {
            this.from = from;
            this.to = to;
            this.cost = cost;
            this.bidirectional = bidirectional;
        }
    }

    /// <summary>导航生成完成后的统计信息，供编辑器和离线流水线显示诊断结果。</summary>
    [Serializable]
    public struct NavBuildReport
    {
        public int inputTriangles;
        public int walkableInputTriangles;
        public int rasterizedCells;
        public int walkableCells;
        public int mergedRectangles;
        public int delaunayRegions;
        public int delaunayFallbackRegions;
        public int outputTriangles;
        public int addedLinks;
        public int rejectedLinks;
    }
}
