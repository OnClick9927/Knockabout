using System;
using System.Collections.Generic;
using Lockstep.RVO;

namespace Lockstep.Nav
{
    /// <summary>
    /// NavMap 全局寻路与 RVO/ORCA 局部避障的纯运行时桥接器。
    /// <para>
    /// 调用方以固定逻辑帧调用 Step：桥接器先让每个 NavRvoAgent 根据路径生成期望速度，
    /// 再统一执行 RVO，最后逐个把积分位置限制回连续 NavMesh 表面。整个流程不引用 Unity，
    /// 也不修改已有 NavMap、Simulator 或 Triangle 数据。
    /// </para>
    /// </summary>
    public sealed class NavRvoWorld
    {
        private readonly List<NavRvoAgent> agents = new List<NavRvoAgent>();
        private readonly Dictionary<int, NavRvoAgent> agentsById = new Dictionary<int, NavRvoAgent>();
        private bool stepping;

        public NavRvoWorld(NavData data)
            : this(data, LFloat.FromRaw(100000L))
        {
        }

        public NavRvoWorld(NavData data, LFloat timeStep)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (timeStep <= LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(timeStep));

            Map = new NavMap(data);
            rvoSimulator = new Simulator
            {
                timeStep_ = timeStep
            };
            Constraint = new NavMeshConstraint(Map, data);
        }

        /// <summary>用于全局路径搜索和三角形高度吸附的导航图。</summary>
        public NavMap Map { get; }

        /// <summary>当前由世界管理的导航移动代理。</summary>
        public IReadOnlyList<NavRvoAgent> Agents => agents;

        /// <summary>当前由世界管理的移动代理数量。</summary>
        public int AgentCount => agents.Count;

        /// <summary>已经提交到底层 RVO 的静态障碍顶点数量。</summary>
        public int ObstacleVertexCount => rvoSimulator.NumObstacleVertices;

        /// <summary>固定逻辑步长；修改后下一次 Step 立即使用新值。</summary>
        public LFloat TimeStep
        {
            get => rvoSimulator.timeStep_;
            set => rvoSimulator.timeStep_ = value;
        }

        internal NavMeshConstraint Constraint { get; }
        private readonly Simulator rvoSimulator;

        /// <summary>
        /// 在 NavMesh 上创建一个 RVO 代理。起点不属于任何导航三角形时抛出异常，
        /// 从而保证所有成功创建并交给调用方的 NavRvoAgent 从第一帧开始就在导航表面上。
        /// </summary>
        public NavRvoAgent AddAgent(
            LVector3 start,
            NavRvoAgentSettings settings = null,
            object userData = null)
        {
            if (!TryAddAgent(start, settings, out NavRvoAgent agent, userData))
                throw new ArgumentException("Agent start is outside the navigation mesh.", nameof(start));
            return agent;
        }

        /// <summary>尝试创建代理；起点不在 NavMesh 上时返回 false 且不修改 RVO 模拟器。</summary>
        public bool TryAddAgent(
            LVector3 start,
            NavRvoAgentSettings settings,
            out NavRvoAgent agent,
            object userData = null)
        {
            ThrowIfStepping();
            agent = null;
            settings = settings ?? new NavRvoAgentSettings();
            settings.Validate();

            if (!Constraint.TryPlace(start, out Triangle triangle, out LVector3 snappedStart))
                return false;

            var rvoPosition = new LVector2(snappedStart.x, snappedStart.z);
            Lockstep.RVO.Agent rvoAgent = rvoSimulator.addAgent(
                rvoPosition,
                settings.neighborDistance,
                settings.maxNeighbors,
                settings.timeHorizon,
                settings.obstacleTimeHorizon,
                settings.radius,
                settings.maxSpeed,
                LVector2.zero);
            agent = new NavRvoAgent(
                this,
                rvoAgent,
                triangle,
                snappedStart,
                settings,
                userData);
            agents.Add(agent);
            agentsById.Add(agent.Id, agent);
            return true;
        }

        /// <summary>
        /// 移除代理。底层 Simulator 会在下一次 Step 开头压缩列表；稳定 Id 不会被其他代理继承。
        /// </summary>
        public bool RemoveAgent(NavRvoAgent agent)
        {
            ThrowIfStepping();
            if (agent == null) return false;

            int index = agents.IndexOf(agent);
            if (index < 0) return false;
            agents.RemoveAt(index);
            agentsById.Remove(agent.Id);
            rvoSimulator.delAgent(agent.Id);
            agent.MarkRemoved();
            return true;
        }

        /// <summary>
        /// 向 RVO 添加一条静态障碍轮廓。坐标使用 NavMesh 相同的 XZ 投影；本方法不改变导航几何，
        /// 因此障碍轮廓应与构建导航时使用的不可行走边界保持一致。
        /// </summary>
        public int AddObstacle(IList<LVector2> vertices)
        {
            ThrowIfStepping();
            return rvoSimulator.addObstacle(vertices);
        }

        /// <summary>立即构建静态障碍 KD 树；也可以让下一次 Step 按需完成。</summary>
        public void ProcessObstacles()
        {
            ThrowIfStepping();
            rvoSimulator.processObstacles();
        }

        /// <summary>使用当前 RVO 静态障碍判断两个 XZ 点之间是否保留指定半径的可见通道。</summary>
        public bool QueryVisibility(LVector2 from, LVector2 to, LFloat radius)
        {
            return rvoSimulator.queryVisibility(from, to, radius);
        }

        /// <summary>
        /// 执行一个确定性逻辑步。调用期间不得通过回调增删代理；方法包含重入保护，
        /// 避免一次尚未提交的 RVO 结果被另一轮 Step 覆盖。
        /// </summary>
        public void Step()
        {
            if (stepping)
                throw new InvalidOperationException("NavRvoWorld.Step cannot be re-entered.");

            stepping = true;
            try
            {
                LFloat timeStep = rvoSimulator.timeStep_;
                for (int i = 0; i < agents.Count; i++)
                    agents[i].PrepareStep(timeStep);

                // 阻挡数必须在任何代理开始退让前统一计算，保证所有代理比较的是同一阶段的数据。
                for (int i = 0; i < agents.Count; i++)
                    agents[i].PrepareCongestionMetrics();

                // 先基于上一帧实际进展和近邻缓存划分局部拥堵组，再由每个组稳定选出一个通行者。
                // 该阶段不直接移动代理，只准备本帧将要提交给 RVO 的退让方向。
                for (int i = 0; i < agents.Count; i++)
                    agents[i].PrepareCongestionResolution(timeStep);

                // 先让所有代理完成原始路径期望速度，再统一应用拥堵破对称。这样每个代理读取到的
                // 邻居方向都来自同一逻辑阶段，结果不会受 agents 列表遍历先后影响。
                for (int i = 0; i < agents.Count; i++)
                    agents[i].ApplyCongestionBias(timeStep);

                rvoSimulator.doStep();

                for (int i = 0; i < agents.Count; i++)
                    agents[i].CommitStep(timeStep);
            }
            finally
            {
                stepping = false;
            }
        }

        /// <summary>
        /// 清空全部桥接代理、RVO 障碍与空间索引。NavMap 是不可变查询对象，可继续用于之后新增代理。
        /// </summary>
        public void Clear()
        {
            ThrowIfStepping();
            for (int i = 0; i < agents.Count; i++)
                agents[i].MarkRemoved();
            agents.Clear();
            agentsById.Clear();
            LFloat timeStep = rvoSimulator.timeStep_;
            rvoSimulator.Clear();
            rvoSimulator.timeStep_ = timeStep;
        }

        /// <summary>把底层 RVO 的稳定编号映射回导航代理，供上一帧近邻缓存复用。</summary>
        internal bool TryGetAgent(int id, out NavRvoAgent agent)
        {
            return agentsById.TryGetValue(id, out agent);
        }

        private void ThrowIfStepping()
        {
            if (stepping)
                throw new InvalidOperationException("Agents cannot be added or removed during NavRvoWorld.Step.");
        }
    }
}
