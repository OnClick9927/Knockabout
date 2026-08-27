using System;
using System.Collections.Generic;
using RvoAgent = Lockstep.RVO.Agent;

namespace Lockstep.Nav
{
    /// <summary>NavMesh 与 RVO 联合移动代理当前所处的流程状态。</summary>
    public enum NavRvoAgentState
    {
        /// <summary>尚未设置目标，代理保持在当前位置参与避障。</summary>
        Idle,
        /// <summary>正在沿 NavMap 生成的路径移动。</summary>
        Moving,
        /// <summary>已经抵达 LinkFrom，等待业务层确认跨越离散链接。</summary>
        WaitingForLink,
        /// <summary>已经抵达最终目标。</summary>
        Arrived,
        /// <summary>最近一次寻路或约束后的重新寻路失败。</summary>
        PathFailed,
        /// <summary>代理已从 NavRvoWorld 移除，不可再次使用。</summary>
        Removed
    }

    /// <summary>
    /// 创建 NavRvoAgent 时使用的确定性参数。
    /// <para>
    /// NavMesh 应当已经按同一个 radius 烘焙净空；这里的 radius 用于 RVO 避让以及普通拐点的
    /// 有效通过范围，不会放宽最终目标或离散 Link 的到达条件。
    /// 所有距离、速度和时间均为 Lockstep 定点值，不依赖 Unity 类型。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class NavRvoAgentSettings
    {
        /// <summary>搜索其他移动代理的最大距离。</summary>
        public LFloat neighborDistance = LFloat.FromRaw(5000000L);
        /// <summary>参与一次 ORCA 求解的最大邻居数量。</summary>
        public int maxNeighbors = 10;
        /// <summary>预测移动代理碰撞的时间视野，必须大于零。</summary>
        public LFloat timeHorizon = LFloat.FromRaw(2000000L);
        /// <summary>预测静态 RVO 障碍碰撞的时间视野，必须大于零。</summary>
        public LFloat obstacleTimeHorizon = LFloat.FromRaw(2000000L);
        /// <summary>RVO 圆形代理半径。</summary>
        public LFloat radius = LFloat.FromRaw(500000L);
        /// <summary>沿路径移动时允许采用的最大速度。</summary>
        public LFloat maxSpeed = LFloat.FromRaw(2000000L);
        /// <summary>
        /// 沿当前路径方向的有效前进速度持续低于 congestionForwardSpeedRatio 配置的比例并达到
        /// 该时间后，即使代理仍在横向抖动，也按拥堵处理；默认速度比例为最大速度的百分之五。
        /// </summary>
        public LFloat congestionDetectionTime = LFloat.FromRaw(250000L);
        /// <summary>
        /// 判断“没有有效前进”时使用的最大速度比例。实际速度在原始路径方向上的投影不高于
        /// maxSpeed * congestionForwardSpeedRatio 时累计拥堵时间；建议范围为 0.01 到 0.2。
        /// </summary>
        public LFloat congestionForwardSpeedRatio = LFloat.FromRaw(50000L);
        /// <summary>
        /// 局部排队组的搜索半径倍率，实际半径为双方组合半径乘以该值。数值越大，越早把附近
        /// Agent 纳入同一排队组；零表示只有中心完全重合时才可能组成排队组。
        /// </summary>
        public LFloat congestionGroupRadiusScale = LFloat.FromRaw(3000000L);
        /// <summary>
        /// 使用双方原始期望速度预测轨迹交点的最长时间。数值越大，侧向引导介入越早；
        /// 零表示不预测未来交点，仅保留持续低速后的拥堵恢复。
        /// </summary>
        public LFloat congestionPredictionTime = LFloat.FromRaw(2000000L);
        /// <summary>
        /// 预测冲突半径附加的最大 Agent 半径比例，用于在真正接触前留出协商空间。
        /// 零表示只使用双方半径之和。
        /// </summary>
        public LFloat congestionConflictMargin = LFloat.FromRaw(500000L);
        /// <summary>
        /// 验证侧向或退让方向时向前预览的逻辑步数；预览距离仍不会小于 Agent 半径。
        /// 数值越大越保守，零表示只按 Agent 半径探测。
        /// </summary>
        public int congestionProbeSteps = 4;
        /// <summary>
        /// 拥堵时附加到原始期望方向右侧的速度比例。所有相向代理采用各自的右侧，会自然分到
        /// 不同通行侧；零表示关闭该功能，建议范围为 0.2 到 0.5。
        /// </summary>
        public LFloat congestionSideBias = LFloat.FromRaw(350000L);
        /// <summary>
        /// 接触拥堵组中非优先代理主动退让时采用的最大速度比例。退让不会改变 NavMap 路径，
        /// 只在局部拥堵解除前临时覆盖 RVO 期望速度；零表示关闭拥堵组排队。
        /// </summary>
        public LFloat congestionYieldSpeed = LFloat.FromRaw(450000L);
        /// <summary>
        /// 一次破对称引导至少保持的时间。短暂保持可防止代理刚产生位移便撤销侧向分量，
        /// 随后重新落回完全对称的零速解；拥堵组中的退让方向也使用同一保持时间。
        /// </summary>
        public LFloat congestionBiasDuration = LFloat.FromRaw(1000000L);
        /// <summary>
        /// XZ 平面中的额外路径点容差。普通漏斗拐点位于导航走廊边界，实际通过距离会再加上
        /// 代理半径；最终目标和 Link 仍只使用该值，避免在离散跳转或终点处提前结束。
        /// </summary>
        public LFloat waypointTolerance = LFloat.FromRaw(50000L);
        /// <summary>发生 NavMesh 边界裁剪后，是否从修正位置自动重新寻路。</summary>
        public bool repathWhenConstrained = true;
        /// <summary>
        /// 是否自动从 LinkFrom 跳到紧随其后的 LinkTo。
        /// 关闭后代理会进入 WaitingForLink，业务层应在动画或跳跃完成时调用 TraverseLink。
        /// </summary>
        public bool autoTraverseLinks = true;

        internal void Validate()
        {
            if (neighborDistance < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(neighborDistance));
            if (maxNeighbors < 0)
                throw new ArgumentOutOfRangeException(nameof(maxNeighbors));
            if (timeHorizon <= LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(timeHorizon));
            if (obstacleTimeHorizon <= LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(obstacleTimeHorizon));
            if (radius < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (maxSpeed < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(maxSpeed));
            if (congestionDetectionTime < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionDetectionTime));
            if (congestionForwardSpeedRatio < LFloat.zero ||
                congestionForwardSpeedRatio > LFloat.one)
                throw new ArgumentOutOfRangeException(nameof(congestionForwardSpeedRatio));
            if (congestionGroupRadiusScale < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionGroupRadiusScale));
            if (congestionPredictionTime < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionPredictionTime));
            if (congestionConflictMargin < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionConflictMargin));
            if (congestionProbeSteps < 0)
                throw new ArgumentOutOfRangeException(nameof(congestionProbeSteps));
            if (congestionSideBias < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionSideBias));
            if (congestionYieldSpeed < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionYieldSpeed));
            if (congestionBiasDuration < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(congestionBiasDuration));
            if (waypointTolerance < LFloat.zero)
                throw new ArgumentOutOfRangeException(nameof(waypointTolerance));
        }
    }

    /// <summary>
    /// 同时受 NavMap 路径和 RVO 局部避障驱动的移动代理。
    /// <para>
    /// 外部只读取三维 Position；二维 RVO 位置由 NavRvoWorld 管理。每个固定步结束后，候选位置
    /// 都会被吸附或裁剪回导航三角形，并同步写回 RVO，因此后续邻居查询不会读取越界位置。
    /// </para>
    /// </summary>
    public sealed class NavRvoAgent
    {
        private readonly NavRvoWorld world;
        private readonly RvoAgent rvoAgent;
        private readonly List<NavPathPoint> path = new List<NavPathPoint>();
        private readonly LFloat radius;
        private readonly LFloat maxSpeed;
        private readonly LFloat congestionDetectionTime;
        private readonly LFloat congestionForwardSpeedRatio;
        private readonly LFloat congestionGroupRadiusScale;
        private readonly LFloat congestionPredictionTime;
        private readonly LFloat congestionConflictMargin;
        private readonly int congestionProbeSteps;
        private readonly LFloat congestionSideBias;
        private readonly LFloat congestionYieldSpeed;
        private readonly LFloat congestionBiasDuration;
        private readonly LFloat waypointTolerance;
        private readonly bool repathWhenConstrained;
        private readonly bool autoTraverseLinks;

        private Triangle currentTriangle;
        private LVector3 destination;
        private int pathIndex;
        private int cachedSteeringTargetIndex = -1;
        private LVector3 cachedSteeringTarget;
        private LVector2 basePreferredVelocity;
        private LFloat lowSpeedElapsed;
        private LFloat congestionBiasRemaining;
        private LFloat congestionYieldRemaining;
        private LVector2 congestionYieldDirection;
        private int congestionBlockerCount;

        internal NavRvoAgent(
            NavRvoWorld world,
            RvoAgent rvoAgent,
            Triangle triangle,
            LVector3 position,
            NavRvoAgentSettings settings,
            object userData)
        {
            this.world = world;
            this.rvoAgent = rvoAgent;
            currentTriangle = triangle;
            Position = position;
            radius = settings.radius;
            maxSpeed = settings.maxSpeed;
            congestionDetectionTime = settings.congestionDetectionTime;
            congestionForwardSpeedRatio = settings.congestionForwardSpeedRatio;
            congestionGroupRadiusScale = settings.congestionGroupRadiusScale;
            congestionPredictionTime = settings.congestionPredictionTime;
            congestionConflictMargin = settings.congestionConflictMargin;
            congestionProbeSteps = settings.congestionProbeSteps;
            congestionSideBias = settings.congestionSideBias;
            congestionYieldSpeed = settings.congestionYieldSpeed;
            congestionBiasDuration = settings.congestionBiasDuration;
            waypointTolerance = settings.waypointTolerance;
            repathWhenConstrained = settings.repathWhenConstrained;
            autoTraverseLinks = settings.autoTraverseLinks;
            UserData = userData;
            State = NavRvoAgentState.Idle;
        }

        /// <summary>代理中心在导航网格表面上的三维位置，Y 已吸附到当前三角形平面。</summary>
        public LVector3 Position { get; private set; }

        /// <summary>RVO 在上一个固定步实际采用的 XZ 平面速度。</summary>
        public LVector2 Velocity => rvoAgent.velocity_;

        /// <summary>最近一次成功路径的最终吸附目标。</summary>
        public LVector3 Destination => destination;

        /// <summary>当前移动流程状态。</summary>
        public NavRvoAgentState State { get; private set; }

        /// <summary>由调用方保存的业务对象；桥接层不读取或管理其生命周期。</summary>
        public object UserData { get; }

        /// <summary>RVO 分配的稳定编号，不会因其他代理被删除而变化。</summary>
        public int Id => rvoAgent.id_;

        /// <summary>当前路径的只读视图；SetDestination 和自动重寻路会原地更新同一个列表。</summary>
        public IReadOnlyList<NavPathPoint> Path => path;

        /// <summary>下一项尚未完成的路径点下标。</summary>
        public int PathIndex => pathIndex;

        /// <summary>代理是否仍有一条正在执行或等待 Link 的有效路径。</summary>
        public bool HasPath =>
            State == NavRvoAgentState.Moving ||
            State == NavRvoAgentState.WaitingForLink;

        /// <summary>
        /// 通过 NavMap 搜索到目标的路径。目标的 XZ 投影必须落在导航网格内；输入 Y 可以不精确，
        /// 桥接层会先选择高度最近的三角形并吸附到其平面。
        /// </summary>
        public NavResult SetDestination(LVector3 target)
        {
            ThrowIfRemoved();
            if (!world.Constraint.TryPlace(target, out Triangle _, out LVector3 snappedTarget))
            {
                StopWithState(NavRvoAgentState.PathFailed);
                return NavResult.EndNotInNavMesh;
            }

            destination = snappedTarget;
            return RebuildPath();
        }

        /// <summary>清除当前路径并把代理固定在当前导航位置；代理仍保留在 RVO 中供其他代理避让。</summary>
        public void Stop()
        {
            ThrowIfRemoved();
            StopWithState(NavRvoAgentState.Idle);
        }

        /// <summary>
        /// 把代理传送到另一个导航点。目标必须落在 NavMesh 上；成功后会清除原路径，
        /// 同时重置 RVO 实际速度，防止下一步继承传送前的运动状态。
        /// </summary>
        public bool TryWarp(LVector3 target)
        {
            ThrowIfRemoved();
            if (!world.Constraint.TryPlace(target, out Triangle triangle, out LVector3 snapped))
                return false;

            SetPosition(triangle, snapped, true);
            StopWithState(NavRvoAgentState.Idle);
            return true;
        }

        /// <summary>
        /// 把代理从孔洞边界、退化共享顶点或累计定点误差位置恢复到当前合法三角形的稳定内部点。
        /// <para>
        /// 恢复会清除 RVO 的实际速度；若代理原本拥有目标，则从恢复点重新搜索到同一目标的路径。
        /// 该方法不执行全局最近点传送，因此不会跨越墙体、孔洞或不连通的导航岛。
        /// </para>
        /// </summary>
        public bool TryRecoverToNavMesh()
        {
            ThrowIfRemoved();
            if (!world.Constraint.TryGetRecoveryPoint(currentTriangle, out LVector3 recoveryPoint))
                return false;

            bool shouldResumePath =
                State == NavRvoAgentState.Moving ||
                State == NavRvoAgentState.Arrived ||
                State == NavRvoAgentState.PathFailed;
            SetPosition(currentTriangle, recoveryPoint, true);
            if (!shouldResumePath)
                return true;

            return RebuildPath() == NavResult.Success;
        }

        /// <summary>
        /// 手动跨越当前路径中的离散链接。仅 WaitingForLink 状态有效；出口会再次通过 NavMesh
        /// 包含测试和高度吸附，失败时保持入口位置并进入 PathFailed。
        /// </summary>
        public bool TraverseLink()
        {
            ThrowIfRemoved();
            if (State != NavRvoAgentState.WaitingForLink ||
                pathIndex < 0 ||
                pathIndex + 1 >= path.Count ||
                path[pathIndex].type != NavPathPoint.PointType.LinkFrom ||
                path[pathIndex + 1].type != NavPathPoint.PointType.LinkTo)
                return false;

            if (!WarpAcrossLink(path[pathIndex + 1].position))
            {
                StopWithState(NavRvoAgentState.PathFailed);
                return false;
            }

            pathIndex += 2;
            State = NavRvoAgentState.Moving;
            AdvanceReachedPathPoints();
            return State != NavRvoAgentState.PathFailed;
        }

        /// <summary>在 RVO 求解前把下一路径点转换为期望速度。</summary>
        internal void PrepareStep(LFloat timeStep)
        {
            if (State == NavRvoAgentState.Removed)
                return;

            if (State != NavRvoAgentState.Moving)
            {
                SetBasePreferredVelocity(LVector2.zero);
                return;
            }

            AdvanceReachedPathPoints();
            if (State != NavRvoAgentState.Moving || pathIndex >= path.Count)
            {
                SetBasePreferredVelocity(LVector2.zero);
                return;
            }

            LVector2 current = ToPlanar(Position);
            // NavMap 的漏斗结果按点代理生成，普通拐点可能正好落在障碍尖角。圆形代理直接朝尖角
            // 移动时，期望速度会持续指向膨胀障碍内部；改用半径外扩后的控制点，给 ORCA 一个明确
            // 的切向分量，使代理能连续绕过拐角而不是在零速附近左右切换。
            LVector2 target = ToPlanar(GetSteeringTarget(pathIndex));
            LVector2 delta = target - current;
            LFloat distance = delta.magnitude;
            if (distance <= LFloat.EPSILON || maxSpeed <= LFloat.zero)
            {
                SetBasePreferredVelocity(LVector2.zero);
                return;
            }

            // 接近拐点时限制速度，保证单步位移不会主动越过路径点。
            LFloat arrivalSpeed = distance / timeStep;
            LFloat speed = LMath.Min(maxSpeed, arrivalSpeed);
            SetBasePreferredVelocity(delta / distance * speed);
        }

        /// <summary>
        /// 统计当前路径方向上已经接触的移动代理。拥堵组优先放行阻挡者最少的前排代理，
        /// 使通行权随队首离开自然向后传递；稳定编号只处理阻挡数完全相同的交叉流。
        /// </summary>
        internal void PrepareCongestionMetrics()
        {
            congestionBlockerCount = 0;
            if (State != NavRvoAgentState.Moving ||
                basePreferredVelocity.sqrMagnitude <= LFloat.EPSILON)
                return;

            LVector2 position = ToPlanar(Position);
            List<KeyValuePair<LFloat, RvoAgent>> neighbors = rvoAgent.agentNeighbors_;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!world.TryGetAgent(neighbors[i].Value.id_, out NavRvoAgent other) ||
                    other.State != NavRvoAgentState.Moving)
                    continue;

                LFloat combinedRadius = radius + other.radius;
                LFloat groupRadius = combinedRadius * congestionGroupRadiusScale;
                LVector2 offset = ToPlanar(other.Position) - position;
                if (offset.sqrMagnitude <= groupRadius * groupRadius &&
                    LVector2.Dot(offset, basePreferredVelocity) > LFloat.zero)
                    congestionBlockerCount++;
            }
        }

        /// <summary>
        /// 在已接触的局部拥堵组中选出路径前方阻挡者最少的通行代理，其余代理暂时远离该代理。
        /// <para>
        /// 仅旋转期望速度不足以解决障碍拐角处的密集拥堵：后排持续向前时，ORCA 可以长期得到
        /// 一组带有微小横向抖动、却没有路径净进展的速度。让同一个局部组每次只保留一个明确的
        /// 通行者，可以把不可协商的双向挤压转换成有序排队。稳定编号只用于阻挡数相同的平局，
        /// 不依赖容器遍历顺序；所有位置和计时仍为定点值。
        /// </para>
        /// </summary>
        internal void PrepareCongestionResolution(LFloat timeStep)
        {
            if (State != NavRvoAgentState.Moving ||
                congestionYieldSpeed <= LFloat.zero ||
                basePreferredVelocity.sqrMagnitude <= LFloat.EPSILON)
            {
                congestionYieldRemaining = LFloat.zero;
                congestionYieldDirection = LVector2.zero;
                return;
            }

            congestionYieldRemaining = LMath.Max(
                LFloat.zero,
                congestionYieldRemaining - timeStep);

            bool groupIsCongested = IsPersistentlyCongested;
            NavRvoAgent priorityAgent = this;
            LVector2 position = ToPlanar(Position);
            List<KeyValuePair<LFloat, RvoAgent>> neighbors = rvoAgent.agentNeighbors_;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!world.TryGetAgent(neighbors[i].Value.id_, out NavRvoAgent other) ||
                    other.State != NavRvoAgentState.Moving ||
                    other.basePreferredVelocity.sqrMagnitude <= LFloat.EPSILON)
                    continue;

                // 只把已经接触或即将接触的代理归入同一个排队组，避免远处某个较小 Id
                // 让整条走廊都提前后退。倍率由 settings 控制，默认值可连接紧密相邻的拥堵团。
                LFloat combinedRadius = radius + other.radius;
                LFloat groupRadius = combinedRadius * congestionGroupRadiusScale;
                LVector2 offset = ToPlanar(other.Position) - position;
                if (offset.sqrMagnitude > groupRadius * groupRadius)
                    continue;

                groupIsCongested |= other.IsPersistentlyCongested;
                if (HasHigherCongestionPriority(other, priorityAgent))
                    priorityAgent = other;
            }

            if (!groupIsCongested)
                return;

            if (priorityAgent == this)
            {
                // 每个局部组必须始终有一个不退让的通行者，否则保持计时会让新旧组长一起后退。
                congestionYieldRemaining = LFloat.zero;
                congestionYieldDirection = LVector2.zero;
                return;
            }

            LVector2 away = position - ToPlanar(priorityAgent.Position);
            LFloat awayLength = away.magnitude;
            congestionYieldDirection = awayLength > LFloat.EPSILON
                ? away / awayLength
                : -basePreferredVelocity / basePreferredVelocity.magnitude;
            congestionYieldRemaining = congestionBiasDuration;
        }

        private static bool HasHigherCongestionPriority(
            NavRvoAgent candidate,
            NavRvoAgent current)
        {
            if (candidate.congestionBlockerCount != current.congestionBlockerCount)
                return candidate.congestionBlockerCount < current.congestionBlockerCount;
            return candidate.Id < current.Id;
        }

        /// <summary>
        /// 对持续低速且预测轨迹会与其他代理相交的情况施加确定性的靠右侧向引导。
        /// <para>
        /// 标准 ORCA 在完全对称输入下可能得到“所有代理同时停下”的合法最优解。该方法不修改
        /// NavMap 路径，也不直接移动代理，只在 ORCA 求解前轻微旋转期望速度；最终速度仍必须满足
        /// ORCA 半平面，积分位置也仍由 NavMeshConstraint 验证。
        /// </para>
        /// </summary>
        internal void ApplyCongestionBias(LFloat timeStep)
        {
            if (State != NavRvoAgentState.Moving)
                return;

            LFloat speed = basePreferredVelocity.magnitude;
            if (speed <= LFloat.EPSILON)
                return;

            if (congestionYieldRemaining > LFloat.zero &&
                ApplyCongestionYield(speed, timeStep))
                return;

            if (congestionSideBias <= LFloat.zero)
                return;

            bool hasConvergingAgent = HasConvergingAgent();
            if (!hasConvergingAgent && congestionBiasRemaining <= LFloat.zero)
                return;

            // 预测到冲突时立即开始并刷新保持时间，不能等代理已经围住共享点后才处理。
            if (hasConvergingAgent)
                congestionBiasRemaining = congestionBiasDuration;

            LVector2 direction = basePreferredVelocity / speed;
            LVector2 right = new LVector2(direction.y, -direction.x);
            if (!CanSteer(right, timeStep))
            {
                // 靠右会立即越出导航面时尝试另一侧。该回退只用于贴外边界或极窄走廊；
                // 开放区域中的所有代理仍统一靠右，从而保持确定性的通行约定。
                right = -right;
                if (!CanSteer(right, timeStep))
                    return;
            }

            LVector2 biasedVelocity =
                basePreferredVelocity + right * (speed * congestionSideBias);
            LFloat biasedSpeed = biasedVelocity.magnitude;
            if (biasedSpeed <= LFloat.EPSILON)
                return;

            // 只旋转方向，不提高原始路径跟随速度，避免拥堵恢复改变最大速度语义。
            rvoAgent.prefVelocity_ = biasedVelocity / biasedSpeed * speed;
        }

        /// <summary>按“远离组内通行者、沿来路后退、左右疏散”的顺序选择第一个合法退让方向。</summary>
        private bool ApplyCongestionYield(LFloat originalSpeed, LFloat timeStep)
        {
            LFloat yieldSpeed = LMath.Min(
                originalSpeed,
                maxSpeed * congestionYieldSpeed);
            if (yieldSpeed <= LFloat.EPSILON)
                return false;

            LVector2 direction = congestionYieldDirection;
            if (TrySetYieldVelocity(direction, yieldSpeed, timeStep))
                return true;

            direction = -basePreferredVelocity / originalSpeed;
            if (TrySetYieldVelocity(direction, yieldSpeed, timeStep))
                return true;

            LVector2 right = new LVector2(direction.y, -direction.x);
            return TrySetYieldVelocity(right, yieldSpeed, timeStep) ||
                   TrySetYieldVelocity(-right, yieldSpeed, timeStep);
        }

        private bool TrySetYieldVelocity(
            LVector2 direction,
            LFloat speed,
            LFloat timeStep)
        {
            LFloat length = direction.magnitude;
            if (length <= LFloat.EPSILON)
                return false;

            direction /= length;
            if (!CanSteer(direction, timeStep))
                return false;

            rvoAgent.prefVelocity_ = direction * speed;
            return true;
        }

        /// <summary>在 RVO 积分后把候选位置约束回当前或相邻导航三角形。</summary>
        internal void CommitStep(LFloat timeStep)
        {
            if (State == NavRvoAgentState.Removed)
                return;

            // Idle、WaitingForLink、Arrived 和 PathFailed 都作为静止代理参与避让，
            // 但自身位置不允许被 ORCA 推离权威导航位置。
            if (State != NavRvoAgentState.Moving)
            {
                WriteRvoPosition(Position);
                rvoAgent.velocity_ = LVector2.zero;
                ResetCongestionState();
                return;
            }

            LVector3 previous = Position;
            LVector3 candidate = new LVector3(
                rvoAgent.position_.x,
                previous.y,
                rvoAgent.position_.y);
            if (!world.Constraint.TryConstrainMove(
                    currentTriangle,
                    previous,
                    candidate,
                    out Triangle triangle,
                    out LVector3 constrainedPosition,
                    out bool constrained))
            {
                // 正常创建的代理一定有 currentTriangle；该分支只防御外部 NavData 被运行时破坏。
                SetPosition(currentTriangle, previous, true);
                StopWithState(NavRvoAgentState.PathFailed);
                return;
            }

            SetPosition(triangle, constrainedPosition, false);
            if (!EnsureCommittedPositionIsNavigable())
            {
                StopWithState(NavRvoAgentState.PathFailed);
                return;
            }
            rvoAgent.velocity_ = (ToPlanar(Position) - ToPlanar(previous)) / timeStep;
            UpdateCongestionState(timeStep);

            if (constrained && repathWhenConstrained)
            {
                // RVO 把代理推到路径走廊外侧时，从裁剪后的合法位置重新生成全局路径。
                // 若目标暂时不可达则停在边界，不继续积累指向网格外的速度。
                RebuildPath();
            }

            AdvanceReachedPathPoints();
        }

        /// <summary>
        /// 防御性验证刚提交的位置仍属于 NavMesh。正常路径只需把 Y 和当前三角形统一到全局查询结果；
        /// 若定点边界误差已经让 XZ 落到孔洞或外部，则回到此前合法三角形内部并重新搜索原目标。
        /// </summary>
        private bool EnsureCommittedPositionIsNavigable()
        {
            if (world.Constraint.TryPlace(Position, out Triangle verifiedTriangle, out LVector3 verifiedPosition))
            {
                SetPosition(verifiedTriangle, verifiedPosition, false);
                return true;
            }

            if (!world.Constraint.TryGetRecoveryPoint(currentTriangle, out LVector3 recoveryPoint))
                return false;

            SetPosition(currentTriangle, recoveryPoint, true);
            return !repathWhenConstrained || RebuildPath() == NavResult.Success;
        }

        internal void MarkRemoved()
        {
            path.Clear();
            pathIndex = 0;
            rvoAgent.prefVelocity_ = LVector2.zero;
            rvoAgent.velocity_ = LVector2.zero;
            ResetCongestionState();
            State = NavRvoAgentState.Removed;
        }

        /// <summary>
        /// 判断当前原始期望轨迹是否会在短时间内与另一移动代理进入组合半径范围。
        /// 使用所有代理本帧尚未加偏的 basePreferredVelocity，结果不受 NavRvoWorld 遍历顺序影响。
        /// </summary>
        private bool HasConvergingAgent()
        {
            // RVO 每帧已经通过 KD 树把近邻裁剪到 maxNeighbors；复用上一帧结果可把额外工作量
            // 从全世界 O(N²) 降到 O(N * maxNeighbors)，且不会产生临时集合或额外 GC。
            List<KeyValuePair<LFloat, RvoAgent>> neighbors = rvoAgent.agentNeighbors_;
            LVector2 position = ToPlanar(Position);
            for (int i = 0; i < neighbors.Count; i++)
            {
                RvoAgent otherRvoAgent = neighbors[i].Value;
                if (!world.TryGetAgent(otherRvoAgent.id_, out NavRvoAgent other) ||
                    other.State != NavRvoAgentState.Moving ||
                    other.basePreferredVelocity.sqrMagnitude <= LFloat.EPSILON)
                    continue;

                LVector2 relativePosition = ToPlanar(other.Position) - position;
                LVector2 relativeVelocity = basePreferredVelocity - other.basePreferredVelocity;
                LFloat relativeSpeedSquared = relativeVelocity.sqrMagnitude;
                if (relativeSpeedSquared <= LFloat.EPSILON)
                    continue;

                LFloat closingProjection = LVector2.Dot(relativePosition, relativeVelocity);
                if (closingProjection <= LFloat.zero)
                    continue;

                LFloat timeToClosestPoint = closingProjection / relativeSpeedSquared;
                if (timeToClosestPoint > congestionPredictionTime)
                    continue;

                LVector2 closestOffset =
                    relativePosition - relativeVelocity * timeToClosestPoint;
                LFloat combinedRadius = radius + other.radius;
                LFloat conflictRadius =
                    combinedRadius +
                    LMath.Max(radius, other.radius) * congestionConflictMargin;
                if (closestOffset.sqrMagnitude <= conflictRadius * conflictRadius)
                    return true;
            }

            return false;
        }

        /// <summary>验证侧向引导在数个逻辑步的预览距离内不会直接碰到 NavMesh 孔洞或外边界。</summary>
        private bool CanSteer(LVector2 direction, LFloat timeStep)
        {
            LFloat probeDistance = LMath.Max(
                radius,
                maxSpeed * timeStep * (LFloat)congestionProbeSteps);
            var probe = new LVector3(
                Position.x + direction.x * probeDistance,
                Position.y,
                Position.z + direction.y * probeDistance);
            return world.Constraint.TryConstrainMove(
                       currentTriangle,
                       Position,
                       probe,
                       out Triangle _,
                       out LVector3 _,
                       out bool constrained) &&
                   !constrained;
        }

        private void SetBasePreferredVelocity(LVector2 velocity)
        {
            basePreferredVelocity = velocity;
            rvoAgent.prefVelocity_ = velocity;
        }

        /// <summary>根据本帧实际提交速度更新拥堵检测与破对称保持时间。</summary>
        private void UpdateCongestionState(LFloat timeStep)
        {
            LFloat preferredSpeed = basePreferredVelocity.magnitude;
            LFloat forwardSpeed = preferredSpeed > LFloat.EPSILON
                ? LVector2.Dot(rvoAgent.velocity_, basePreferredVelocity) / preferredSpeed
                : LFloat.zero;
            LFloat lowForwardSpeedThreshold =
                maxSpeed * congestionForwardSpeedRatio;
            if (forwardSpeed <= lowForwardSpeedThreshold)
            {
                lowSpeedElapsed += timeStep;
                if (lowSpeedElapsed >= congestionDetectionTime)
                    congestionBiasRemaining = congestionBiasDuration;
                return;
            }

            lowSpeedElapsed = LFloat.zero;
            congestionBiasRemaining = LMath.Max(
                LFloat.zero,
                congestionBiasRemaining - timeStep);
        }

        private void ResetCongestionState()
        {
            basePreferredVelocity = LVector2.zero;
            lowSpeedElapsed = LFloat.zero;
            congestionBiasRemaining = LFloat.zero;
            congestionYieldRemaining = LFloat.zero;
            congestionYieldDirection = LVector2.zero;
            congestionBlockerCount = 0;
        }

        /// <summary>代理是否已经持续缺少沿当前路径方向的有效进展。</summary>
        private bool IsPersistentlyCongested =>
            lowSpeedElapsed >= congestionDetectionTime;

        private NavResult RebuildPath()
        {
            // Search 会原地覆盖 path；即使重建后的 pathIndex 与上一条路径相同，旧控制点也已失效。
            cachedSteeringTargetIndex = -1;
            NavResult result = world.Map.Search(Position, destination, path);
            if (result != NavResult.Success || path.Count == 0)
            {
                StopWithState(NavRvoAgentState.PathFailed);
                return result;
            }

            // Search 的第 0 项是吸附后的 Start，当前位置已经位于该点所在三角形中。
            pathIndex = path.Count > 1 ? 1 : 0;
            State = NavRvoAgentState.Moving;
            AdvanceReachedPathPoints();
            return NavResult.Success;
        }

        /// <summary>跳过已抵达的普通拐点，并处理 End 与成对的 LinkFrom/LinkTo。</summary>
        private void AdvanceReachedPathPoints()
        {
            int guard = path.Count + 1;
            while (State == NavRvoAgentState.Moving && pathIndex < path.Count && guard-- > 0)
            {
                NavPathPoint point = path[pathIndex];
                if (!HasReachedPathPoint(point))
                    return;

                if (point.type == NavPathPoint.PointType.End)
                {
                    if (world.Constraint.TryPlace(point.position, out Triangle triangle, out LVector3 snapped))
                        SetPosition(triangle, snapped, true);
                    pathIndex = path.Count;
                    State = NavRvoAgentState.Arrived;
                    rvoAgent.prefVelocity_ = LVector2.zero;
                    rvoAgent.velocity_ = LVector2.zero;
                    ResetCongestionState();
                    return;
                }

                if (point.type == NavPathPoint.PointType.LinkFrom)
                {
                    if (pathIndex + 1 >= path.Count ||
                        path[pathIndex + 1].type != NavPathPoint.PointType.LinkTo)
                    {
                        StopWithState(NavRvoAgentState.PathFailed);
                        return;
                    }

                    // 先固定到链接入口，避免容差范围内触发时留下一个浮动的起跳位置。
                    if (world.Constraint.TryPlace(point.position, out Triangle fromTriangle, out LVector3 from))
                        SetPosition(fromTriangle, from, true);

                    if (!autoTraverseLinks)
                    {
                        State = NavRvoAgentState.WaitingForLink;
                        rvoAgent.prefVelocity_ = LVector2.zero;
                        rvoAgent.velocity_ = LVector2.zero;
                        ResetCongestionState();
                        return;
                    }

                    if (!WarpAcrossLink(path[pathIndex + 1].position))
                    {
                        StopWithState(NavRvoAgentState.PathFailed);
                        return;
                    }
                    pathIndex += 2;
                    continue;
                }

                // LinkTo 通常已在跨越 LinkFrom 时一起消费；遇到孤立 LinkTo 属于无效路径数据。
                if (point.type == NavPathPoint.PointType.LinkTo)
                {
                    StopWithState(NavRvoAgentState.PathFailed);
                    return;
                }

                pathIndex++;
            }

            if (State == NavRvoAgentState.Moving && pathIndex >= path.Count)
                StopWithState(NavRvoAgentState.Arrived);
        }

        /// <summary>
        /// 普通漏斗拐点通常正好位于 Portal 端点或障碍边界。圆形代理的中心受 RVO 障碍膨胀约束，
        /// 不可能精确走到这样的点；若仍只使用很小的数值容差，代理会持续朝不可达点施加期望速度，
        /// 在障碍拐角处形成零速与反向修正交替的抖动。
        ///
        /// 对普通拐点加入代理半径，表示代理圆已经接触并绕过该拐点即可切换到下一段。终点与 Link
        /// 具有明确的业务坐标，仍保持调用方配置的精确容差，不改变到达和离散跳转语义。
        /// </summary>
        private bool HasReachedPathPoint(NavPathPoint point)
        {
            LVector3 steeringTarget = GetSteeringTarget(pathIndex);
            LFloat distance = PlanarDistance(Position, steeringTarget);
            if (distance <= waypointTolerance)
                return true;

            // 没能构造合法圆角控制点时，仍允许圆形代理在自身半径内通过普通漏斗拐点。
            // 最终目标和 Link 不走该分支，继续保持精确到达语义。
            if (point.type != NavPathPoint.PointType.Point ||
                steeringTarget != point.position ||
                distance > radius + waypointTolerance)
                return false;

            // ORCA 负责圆形代理相对实体障碍的净空，NavMeshConstraint 负责中心轨迹不能穿过孔洞；
            // 因此切换期望方向本身不需要额外执行可见性或直线穿越测试。矩形障碍的 RVO 膨胀角是
            // 圆弧，而导航孔洞角通常是直角，要求下一段提前完全可达反而会让代理卡在圆弧切点。
            return true;
        }

        /// <summary>
        /// 为普通漏斗拐点构造适合圆形代理的外侧控制点。设进入方向为 incoming、离开方向为
        /// outgoing，则 incoming - outgoing 指向转弯外侧。例如先向北再向西时，它指向东北，
        /// 正好把代理中心从西南侧障碍的膨胀圆角外推一个半径。
        ///
        /// 控制点只影响本地期望速度，不写回 NavMap 返回的路径。候选点必须仍能由 NavMesh 查询
        /// 命中；位于外边界、狭窄通道或退化折线时退回原始路径点，由既有半径容差处理。
        /// </summary>
        private LVector3 GetSteeringTarget(int pointIndex)
        {
            if (cachedSteeringTargetIndex == pointIndex)
                return cachedSteeringTarget;

            cachedSteeringTarget = CalculateSteeringTarget(pointIndex);
            cachedSteeringTargetIndex = pointIndex;
            return cachedSteeringTarget;
        }

        /// <summary>计算一次当前路径段的圆角控制点；结果由 GetSteeringTarget 缓存到路径切换为止。</summary>
        private LVector3 CalculateSteeringTarget(int pointIndex)
        {
            NavPathPoint point = path[pointIndex];
            if (point.type != NavPathPoint.PointType.Point ||
                pointIndex <= 0 ||
                pointIndex + 1 >= path.Count ||
                radius <= LFloat.EPSILON)
                return point.position;

            LVector2 corner = ToPlanar(point.position);
            LVector2 incoming = corner - ToPlanar(path[pointIndex - 1].position);
            LVector2 outgoing = ToPlanar(path[pointIndex + 1].position) - corner;
            LFloat incomingLength = incoming.magnitude;
            LFloat outgoingLength = outgoing.magnitude;
            if (incomingLength <= NavHelper.epsilon || outgoingLength <= NavHelper.epsilon)
                return point.position;

            incoming /= incomingLength;
            outgoing /= outgoingLength;
            LVector2 outside = incoming - outgoing;
            LFloat outsideLength = outside.magnitude;
            // 崎岖表面展开后，XZ 上原本共线的两段可能残留千分位级方向差。若直接归一化这个
            // 极小向量，会把数值噪声放大成完整半径的横向偏移。小于约 0.01 弧度的折线按直线处理，
            // 此时既无需绕角，也仍会由 HasReachedPathPoint 的半径容差正常消费该点。
            if (outsideLength <= NavHelper.epsilon * LFloat.FromRaw(100000000L))
                return point.position;

            outside /= outsideLength;
            var candidate = new LVector3(
                point.position.x + outside.x * radius,
                point.position.y,
                point.position.z + outside.y * radius);
            return world.Constraint.TryPlace(
                candidate,
                out Triangle _,
                out LVector3 snappedCandidate)
                ? snappedCandidate
                : point.position;
        }

        private bool WarpAcrossLink(LVector3 linkTo)
        {
            if (!world.Constraint.TryPlace(linkTo, out Triangle triangle, out LVector3 snapped))
                return false;

            SetPosition(triangle, snapped, true);
            ResetCongestionState();
            return true;
        }

        private void SetPosition(Triangle triangle, LVector3 position, bool resetVelocity)
        {
            currentTriangle = triangle;
            Position = position;
            WriteRvoPosition(position);
            if (resetVelocity)
                rvoAgent.velocity_ = LVector2.zero;
        }

        private void WriteRvoPosition(LVector3 position)
        {
            rvoAgent.position_ = ToPlanar(position);
        }

        private void StopWithState(NavRvoAgentState state)
        {
            path.Clear();
            pathIndex = 0;
            rvoAgent.prefVelocity_ = LVector2.zero;
            rvoAgent.velocity_ = LVector2.zero;
            WriteRvoPosition(Position);
            ResetCongestionState();
            State = state;
        }

        private void ThrowIfRemoved()
        {
            if (State == NavRvoAgentState.Removed)
                throw new InvalidOperationException("The NavRvoAgent has been removed from its world.");
        }

        private static LVector2 ToPlanar(LVector3 point)
        {
            return new LVector2(point.x, point.z);
        }

        private static LFloat PlanarDistance(LVector3 a, LVector3 b)
        {
            return (ToPlanar(a) - ToPlanar(b)).magnitude;
        }
    }
}
