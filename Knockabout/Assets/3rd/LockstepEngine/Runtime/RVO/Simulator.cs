using System;
using System.Collections.Generic;

namespace Lockstep.RVO
{
    /// <summary>
    /// RVO/ORCA 模拟中的单个圆形代理。
    /// <para>代理先从 KD 树收集有限范围内的障碍和其他代理，再把潜在碰撞转换为速度空间中的
    /// ORCA 半平面约束，最后在最大速度圆内寻找最接近期望速度的可行速度。</para>
    /// <para>所有数值都使用定点数，因而在相同输入和更新顺序下能够得到确定性的结果。</para>
    /// </summary>
    public class Agent
    {
        // 邻障碍较少时直接插入排序，数量较大时末尾统一排序，兼顾常见小集合和密集场景。
        private const int ObstacleSortThreshold = 16;

        private sealed class ObstacleNeighborComparer : IComparer<KeyValuePair<LFloat, Obstacle>>
        {
            internal static readonly ObstacleNeighborComparer Instance = new ObstacleNeighborComparer();

            public int Compare(KeyValuePair<LFloat, Obstacle> left, KeyValuePair<LFloat, Obstacle> right)
            {
                if (left.Key < right.Key)
                    return -1;
                if (left.Key > right.Key)
                    return 1;
                return left.Value.id_.CompareTo(right.Value.id_);
            }
        }

        // Key 保存距离平方，避免邻居搜索和排序阶段反复开平方。
        internal readonly List<KeyValuePair<LFloat, Agent>> agentNeighbors_;
        internal readonly List<KeyValuePair<LFloat, Obstacle>> obstacleNeighbors_;

        /// <summary>本帧生成的 ORCA 约束线；线的左侧为允许速度区域。</summary>
        public readonly List<Line> orcaLines_;
        /// <summary>代理在二维世界平面中的当前位置。</summary>
        public LVector2 position_;
        /// <summary>外部移动逻辑给出的期望速度；避障求解会尽量逼近该速度。</summary>
        public LVector2 prefVelocity_;
        /// <summary>上一帧已经采用的实际速度。</summary>
        public LVector2 velocity_;
        /// <summary>模拟器内稳定递增的代理编号，不等同于列表下标。</summary>
        public int id_ = 0;
        /// <summary>参与代理间避障计算的最大邻居数量。</summary>
        public int maxNeighbors_ = 0;
        /// <summary>速度圆半径，即代理允许的最大移动速度。</summary>
        public LFloat maxSpeed_ = LFloat.zero;
        /// <summary>搜索其他代理时使用的最大邻居距离。</summary>
        public LFloat neighborDist_ = LFloat.zero;
        /// <summary>代理的碰撞半径。</summary>
        public LFloat radius_ = LFloat.zero;
        /// <summary>预测其他代理碰撞所使用的时间视野；越大越早开始绕行。</summary>
        public LFloat timeHorizon_ = LFloat.zero;
        /// <summary>预测静态障碍碰撞所使用的时间视野。</summary>
        public LFloat timeHorizonObst_ = LFloat.zero;
        internal bool needDelete_ = false;
        // newVelocity_ 只在所有代理完成求解后统一提交，避免同一帧内的更新顺序影响结果。
        private LVector2 newVelocity_;
        private readonly List<Line> projectedLines_;
        private bool hasCachedTimeHorizon_;
        private bool hasCachedTimeHorizonObst_;
        private LFloat cachedTimeHorizon_;
        private LFloat cachedTimeHorizonObst_;
        private LFloat invTimeHorizon_;
        private LFloat invTimeHorizonObst_;

        private Simulator simulator;

        public Agent(Simulator simulator)
            : this(simulator, 0)
        {
        }

        internal Agent(Simulator simulator, int maxNeighbors)
        {
            this.simulator = simulator;
            int capacity = maxNeighbors > 0 ? maxNeighbors : 0;
            agentNeighbors_ = new List<KeyValuePair<LFloat, Agent>>(capacity);
            obstacleNeighbors_ = new List<KeyValuePair<LFloat, Obstacle>>();
            orcaLines_ = new List<Line>(capacity);
            projectedLines_ = new List<Line>(capacity);
        }

        /// <summary>取得代理时间视野的倒数，并在参数未变化时复用缓存。</summary>
        private LFloat GetInvTimeHorizon()
        {
            if (!hasCachedTimeHorizon_ || cachedTimeHorizon_ != timeHorizon_)
            {
                cachedTimeHorizon_ = timeHorizon_;
                invTimeHorizon_ = LFloat.one / timeHorizon_;
                hasCachedTimeHorizon_ = true;
            }
            return invTimeHorizon_;
        }

        /// <summary>取得障碍时间视野的倒数，并在参数未变化时复用缓存。</summary>
        private LFloat GetInvTimeHorizonObst()
        {
            if (!hasCachedTimeHorizonObst_ || cachedTimeHorizonObst_ != timeHorizonObst_)
            {
                cachedTimeHorizonObst_ = timeHorizonObst_;
                invTimeHorizonObst_ = LFloat.one / timeHorizonObst_;
                hasCachedTimeHorizonObst_ = true;
            }
            return invTimeHorizonObst_;
        }

        /// <summary>
        /// 从 KD 树收集本帧需要考虑的障碍边和代理。
        /// 障碍搜索半径包含时间视野内最大可移动距离与自身半径；代理搜索则受
        /// <see cref="neighborDist_"/> 和 <see cref="maxNeighbors_"/> 双重限制。
        /// </summary>
        internal void computeNeighbors()
        {
            obstacleNeighbors_.Clear();
            if (simulator.kdTree_.HasObstacleTree)
            {
                LFloat obstacleRangeSq = RVOMath.sqr(timeHorizonObst_ * maxSpeed_ + radius_);
                simulator.kdTree_.computeObstacleNeighbors(this, obstacleRangeSq);
                if (obstacleNeighbors_.Count > ObstacleSortThreshold)
                {
                    obstacleNeighbors_.Sort(ObstacleNeighborComparer.Instance);
                }
            }

            agentNeighbors_.Clear();

            if (maxNeighbors_ > 0)
            {
                LFloat rangeSq = RVOMath.sqr(neighborDist_);
                simulator.kdTree_.computeAgentNeighbors(this, ref rangeSq);
            }
        }

        /// <summary>
        /// 构建本帧的 ORCA 半平面并求出新的无碰撞速度。
        /// <para>先加入不可协商的静态障碍约束，再加入双方各承担一半避让责任的代理约束；
        /// 最后通过二维线性规划，在最大速度圆内选择最接近期望速度的点。</para>
        /// </summary>
        internal void computeNewVelocity()
        {
            orcaLines_.Clear();

            LFloat invTimeHorizonObst = obstacleNeighbors_.Count > 0 ? GetInvTimeHorizonObst() : LFloat.zero;
            LFloat radiusSq = obstacleNeighbors_.Count > 0 ? RVOMath.sqr(radius_) : LFloat.zero;

            // 将每条障碍边膨胀一个代理半径，再把有限时间内的速度障碍投影成约束线。
            for (int i = 0; i < obstacleNeighbors_.Count; ++i)
            {
                Obstacle obstacle1 = obstacleNeighbors_[i].Value;
                Obstacle obstacle2 = obstacle1.next_;

                LVector2 relativePosition1 = obstacle1.point_ - position_;
                LVector2 relativePosition2 = obstacle2.point_ - position_;

                bool alreadyCovered = false;

                for (int j = 0; j < orcaLines_.Count; ++j)
                {
                    if (RVOMath.det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON && RVOMath.det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON)
                    {
                        alreadyCovered = true;
                        break;
                    }
                }

                // 若整条障碍边已落在既有速度障碍后方，则新增约束不会缩小可行域。
                if (alreadyCovered)
                {
                    continue;
                }

                LFloat distSq1 = RVOMath.absSq(relativePosition1);
                LFloat distSq2 = RVOMath.absSq(relativePosition2);

                LVector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
                LFloat s = (LVector2.Dot(-relativePosition1, obstacleVector)) / RVOMath.absSq(obstacleVector);
                LFloat distSqLine = RVOMath.absSq(-relativePosition1 - s * obstacleVector);

                Line line;

                // 已与顶点或边重叠时，约束线穿过速度原点，要求速度立即离开障碍。
                if (s < LFloat.zero && distSq1 <= radiusSq)
                {
                    if (obstacle1.convex_)
                    {
                        line.point = LVector2.zero;
                        line.direction = RVOMath.normalize(new LVector2(-relativePosition1.y, relativePosition1.x));
                        orcaLines_.Add(line);
                    }
                    continue;
                }
                else if (s > LFloat.one && distSq2 <= radiusSq)
                {
                    if (obstacle2.convex_ && RVOMath.det(relativePosition2, obstacle2.direction_) >= LFloat.zero)
                    {
                        line.point = LVector2.zero;
                        line.direction = RVOMath.normalize(new LVector2(-relativePosition2.y, relativePosition2.x));
                        orcaLines_.Add(line);
                    }
                    continue;
                }
                else if (s >= LFloat.zero && s < LFloat.one && distSqLine <= radiusSq)
                {
                    line.point = LVector2.zero;
                    line.direction = -obstacle1.direction_;
                    orcaLines_.Add(line);
                    continue;
                }

                // 未发生重叠时，左右“腿”是当前位置到膨胀障碍轮廓的两条切线。
                LVector2 leftLegDirection, rightLegDirection;

                if (s < LFloat.zero && distSqLine <= radiusSq)
                {
                    if (!obstacle1.convex_)
                    {
                        continue;
                    }

                    obstacle2 = obstacle1;

                    LFloat leg1 = LMath.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new LVector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    rightLegDirection = new LVector2(relativePosition1.x * leg1 + relativePosition1.y * radius_, -relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                }
                else if (s > LFloat.one && distSqLine <= radiusSq)
                {
                    if (!obstacle2.convex_)
                    {
                        continue;
                    }

                    obstacle1 = obstacle2;

                    LFloat leg2 = LMath.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new LVector2(relativePosition2.x * leg2 - relativePosition2.y * radius_, relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new LVector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    if (obstacle1.convex_)
                    {
                        LFloat leg1 = LMath.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new LVector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    }
                    else
                    {
                        leftLegDirection = -obstacle1.direction_;
                    }

                    if (obstacle2.convex_)
                    {
                        LFloat leg2 = LMath.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new LVector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    }
                    else
                    {
                        rightLegDirection = obstacle1.direction_;
                    }
                }

                Obstacle leftNeighbor = obstacle1.previous_;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.convex_ && RVOMath.det(leftLegDirection, -leftNeighbor.direction_) >= LFloat.zero)
                {
                    leftLegDirection = -leftNeighbor.direction_;
                    isLeftLegForeign = true;
                }

                if (obstacle2.convex_ && RVOMath.det(rightLegDirection, obstacle2.direction_) <= LFloat.zero)
                {
                    rightLegDirection = obstacle2.direction_;
                    isRightLegForeign = true;
                }

                LVector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
                LVector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
                LVector2 cutOffVector = rightCutOff - leftCutOff;

                LFloat t = obstacle1 == obstacle2 ? LFloat.half : (LVector2.Dot(velocity_ - leftCutOff, cutOffVector)) / RVOMath.absSq(cutOffVector);
                LFloat tLeft = LVector2.Dot(velocity_ - leftCutOff, leftLegDirection);
                LFloat tRight = LVector2.Dot(velocity_ - rightCutOff, rightLegDirection);

                if ((t < LFloat.zero && tLeft < LFloat.zero) || (obstacle1 == obstacle2 && tLeft < LFloat.zero && tRight < LFloat.zero))
                {
                    LVector2 unitW = RVOMath.normalize(velocity_ - leftCutOff);

                    line.direction = new LVector2(unitW.y, -unitW.x);
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);
                    continue;
                }
                else if (t > LFloat.one && tRight < LFloat.zero)
                {
                    LVector2 unitW = RVOMath.normalize(velocity_ - rightCutOff);

                    line.direction = new LVector2(unitW.y, -unitW.x);
                    line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);
                    continue;
                }

                LFloat distSqCutoff = (t < LFloat.zero || t > LFloat.one || obstacle1 == obstacle2) ? LFloat.FLT_MAX : RVOMath.absSq(velocity_ - (leftCutOff + t * cutOffVector));
                LFloat distSqLeft = tLeft < LFloat.zero ? LFloat.FLT_MAX : RVOMath.absSq(velocity_ - (leftCutOff + tLeft * leftLegDirection));
                LFloat distSqRight = tRight < LFloat.zero ? LFloat.FLT_MAX : RVOMath.absSq(velocity_ - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    line.direction = -obstacle1.direction_;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new LVector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);
                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.direction = leftLegDirection;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new LVector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);
                    continue;
                }

                if (isRightLegForeign)
                {
                    continue;
                }

                line.direction = -rightLegDirection;
                line.point = rightCutOff + radius_ * invTimeHorizonObst * new LVector2(-line.direction.y, line.direction.x);
                orcaLines_.Add(line);
            }

            int numObstLines = orcaLines_.Count;

            LFloat invTimeHorizon = GetInvTimeHorizon();

            for (int i = 0; i < agentNeighbors_.Count; ++i)
            {
                Agent other = agentNeighbors_[i].Value;

                LVector2 relativePosition = other.position_ - position_;
                LVector2 relativeVelocity = velocity_ - other.velocity_;
                LFloat distSq = RVOMath.absSq(relativePosition);
                LFloat combinedRadius = radius_ + other.radius_;
                LFloat combinedRadiusSq = RVOMath.sqr(combinedRadius);

                Line line;
                LVector2 u;

                if (distSq > combinedRadiusSq)
                {
                    LVector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    LFloat wLengthSq = RVOMath.absSq(w);
                    LFloat dotProduct1 = LVector2.Dot(w, relativePosition);

                    if (dotProduct1 < LFloat.zero && RVOMath.sqr(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        LFloat wLength = LMath.Sqrt(wLengthSq);
                        LVector2 unitW = w / wLength;

                        line.direction = new LVector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        LFloat leg = LMath.Sqrt(distSq - combinedRadiusSq);

                        if (RVOMath.det(relativePosition, w) > LFloat.zero)
                        {
                            line.direction = new LVector2(relativePosition.x * leg - relativePosition.y * combinedRadius, relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            line.direction = -new LVector2(relativePosition.x * leg + relativePosition.y * combinedRadius, -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        LFloat dotProduct2 = LVector2.Dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    LFloat invTimeStep = simulator.invTimeStep_;

                    LVector2 w = relativeVelocity - invTimeStep * relativePosition;

                    LFloat wLength = RVOMath.abs(w);
                    LVector2 unitW;
                    if (wLength > RVOMath.RVO_EPSILON)
                    {
                        unitW = w / wLength;
                    }
                    else
                    {
                        unitW = id_ < other.id_ ? LVector2.right : LVector2.left;
                        wLength = LFloat.zero;
                    }

                    line.direction = new LVector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocity_ + LFloat.half * u;
                orcaLines_.Add(line);
            }

            // 首轮求解尽量保持期望速度；若某条代理约束无法同时满足，则进入投影修复。
            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);

            if (lineFail < orcaLines_.Count)
            {
                linearProgram3(orcaLines_, numObstLines, lineFail, maxSpeed_, ref newVelocity_);
            }
        }

        /// <summary>
        /// 尝试插入一个代理邻居，并保持列表按距离平方升序排列。
        /// 当列表达到上限后，<paramref name="rangeSq"/> 会收紧到当前最远邻居，供 KD 树提前剪枝。
        /// </summary>
        internal void insertAgentNeighbor(Agent agent, ref LFloat rangeSq)
        {
            if (this != agent)
            {
                LFloat distSq = RVOMath.absSq(position_ - agent.position_);

                if (distSq < rangeSq)
                {
                    if (agentNeighbors_.Count < maxNeighbors_)
                    {
                        agentNeighbors_.Add(new KeyValuePair<LFloat, Agent>(distSq, agent));
                    }

                    int i = agentNeighbors_.Count - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key)
                    {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<LFloat, Agent>(distSq, agent);

                    if (agentNeighbors_.Count == maxNeighbors_)
                    {
                        rangeSq = agentNeighbors_[agentNeighbors_.Count - 1].Key;
                    }
                }
            }
        }

        /// <summary>
        /// 将指定障碍边加入候选集合。小集合在插入时保持有序，大集合在搜索结束后统一排序。
        /// </summary>
        internal void insertObstacleNeighbor(Obstacle obstacle, LFloat rangeSq)
        {
            Obstacle nextObstacle = obstacle.next_;

            LFloat distSq = RVOMath.distSqPointLineSegment(obstacle.point_, nextObstacle.point_, position_);

            if (distSq < rangeSq)
            {
                obstacleNeighbors_.Add(new KeyValuePair<LFloat, Obstacle>(distSq, obstacle));

                if (obstacleNeighbors_.Count <= ObstacleSortThreshold)
                {
                    int i = obstacleNeighbors_.Count - 1;
                    while (i != 0 && distSq < obstacleNeighbors_[i - 1].Key)
                    {
                        obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                        --i;
                    }
                    obstacleNeighbors_[i] = new KeyValuePair<LFloat, Obstacle>(distSq, obstacle);
                }
            }
        }

        /// <summary>提交已求出的速度，并按模拟步长积分得到下一位置。</summary>
        internal void update()
        {
            velocity_ = newVelocity_;
            position_ += velocity_ * this.simulator.timeStep_;
        }

        /// <summary>
        /// 在指定约束线与最大速度圆的交段上求一维最优解，同时裁剪掉所有先前约束的非法区间。
        /// 返回 <see langword="false"/> 表示该约束线上不存在同时满足先前约束的点。
        /// </summary>
        private bool linearProgram1(List<Line> lines, int lineNo, LFloat radiusSq, LVector2 optVelocity, bool directionOpt, ref LVector2 result)
        {
            Line lineNoValue = lines[lineNo];
            LFloat dotProduct = LVector2.Dot(lineNoValue.point, lineNoValue.direction);
            LFloat discriminant = RVOMath.sqr(dotProduct) + radiusSq - RVOMath.absSq(lineNoValue.point);

            if (discriminant < LFloat.zero)
            {
                return false;
            }

            LFloat sqrtDiscriminant = LMath.Sqrt(discriminant);
            LFloat tLeft = -dotProduct - sqrtDiscriminant;
            LFloat tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                Line otherLine = lines[i];
                LFloat denominator = RVOMath.det(lineNoValue.direction, otherLine.direction);
                LFloat numerator = RVOMath.det(otherLine.direction, lineNoValue.point - otherLine.point);

                if (LMath.Abs(denominator) <= RVOMath.RVO_EPSILON)
                {
                    if (numerator < LFloat.zero)
                    {
                        return false;
                    }
                    continue;
                }

                LFloat t = numerator / denominator;

                if (denominator >= LFloat.zero)
                {
                    tRight = LMath.Min(tRight, t);
                }
                else
                {
                    tLeft = LMath.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                if (LVector2.Dot(optVelocity, lineNoValue.direction) > LFloat.zero)
                {
                    result = lineNoValue.point + tRight * lineNoValue.direction;
                }
                else
                {
                    result = lineNoValue.point + tLeft * lineNoValue.direction;
                }
            }
            else
            {
                LFloat t = LVector2.Dot(lineNoValue.direction, optVelocity - lineNoValue.point);

                if (t < tLeft)
                {
                    result = lineNoValue.point + tLeft * lineNoValue.direction;
                }
                else if (t > tRight)
                {
                    result = lineNoValue.point + tRight * lineNoValue.direction;
                }
                else
                {
                    result = lineNoValue.point + t * lineNoValue.direction;
                }
            }

            return true;
        }

        /// <summary>
        /// 在最大速度圆内依次满足全部半平面约束。
        /// 返回首条无法满足的约束下标；返回约束数量表示求解成功。
        /// </summary>
        private int linearProgram2(List<Line> lines, LFloat radius, LVector2 optVelocity, bool directionOpt, ref LVector2 result)
        {
            LFloat radiusSq = RVOMath.sqr(radius);
            if (directionOpt)
            {
                result = optVelocity * radius;
            }
            else if (RVOMath.absSq(optVelocity) > radiusSq)
            {
                result = RVOMath.normalize(optVelocity) * radius;
            }
            else
            {
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                Line line = lines[i];
                if (RVOMath.det(line.direction, line.point - result) > LFloat.zero)
                {
                    LVector2 tempResult = result;
                    if (!linearProgram1(lines, i, radiusSq, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /// <summary>
        /// 修复第二阶段失败的代理约束。
        /// 障碍约束始终保留；代理约束两两投影为新的边界后，再沿当前失败线的法向寻找可行速度。
        /// 若投影问题仍无解，则保留进入该轮前的结果，保证失败不会扩大约束违反量。
        /// </summary>
        private void linearProgram3(List<Line> lines, int numObstLines, int beginLine, LFloat radius, ref LVector2 result)
        {
            LFloat distance = LFloat.zero;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                Line currentLine = lines[i];
                if (RVOMath.det(currentLine.direction, currentLine.point - result) > distance)
                {
                    projectedLines_.Clear();
                    if (projectedLines_.Capacity < i)
                    {
                        projectedLines_.Capacity = i;
                    }
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projectedLines_.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        Line previousLine = lines[j];
                        LFloat determinant = RVOMath.det(currentLine.direction, previousLine.direction);

                        if (LMath.Abs(determinant) <= RVOMath.RVO_EPSILON)
                        {
                            if (LVector2.Dot(currentLine.direction, previousLine.direction) > LFloat.zero)
                            {
                                continue;
                            }
                            else
                            {
                                line.point = LFloat.half * (currentLine.point + previousLine.point);
                            }
                        }
                        else
                        {
                            line.point = currentLine.point + (RVOMath.det(previousLine.direction, currentLine.point - previousLine.point) / determinant) * currentLine.direction;
                        }

                        line.direction = RVOMath.normalize(previousLine.direction - currentLine.direction);
                        projectedLines_.Add(line);
                    }

                    LVector2 tempResult = result;
                    if (linearProgram2(projectedLines_, radius, new LVector2(-currentLine.direction.y, currentLine.direction.x), true, ref result) < projectedLines_.Count)
                    {
                        result = tempResult;
                    }

                    distance = RVOMath.det(currentLine.direction, currentLine.point - result);
                }
            }
        }
        public int getAgentAgentNeighbor(int neighborNo)
        {
            return agentNeighbors_[neighborNo].Value.id_;
        }
        public int getAgentNumAgentNeighbors()
        {
            return agentNeighbors_.Count;
        }
        public int getAgentNumObstacleNeighbors()
        {
            return obstacleNeighbors_.Count;
        }
        public int getAgentObstacleNeighbor(int neighborNo)
        {
            return obstacleNeighbors_[neighborNo].Value.id_;
        }
    }

    /// <summary>
    /// 二维确定性 RVO/ORCA 模拟器。
    /// <para>负责保存代理与静态障碍、维护空间索引，并以“收集邻居、求解速度、统一积分”的顺序推进一帧。
    /// 统一提交速度可避免列表遍历顺序造成同帧数据污染。</para>
    /// </summary>
    public class Simulator
    {
        public Simulator()
        {
            Clear();
        }
        /// <summary>
        /// 清空全部代理、障碍、索引和默认参数，并把步长恢复为 0.1 秒。
        /// 已分配的障碍节点会归还静态对象池以供后续复用。
        /// </summary>
        public void Clear()
        {
            kdTree_?.reset();

            if (agents_ == null)
                agents_ = new List<Agent>();
            else
                agents_.Clear();

            if (agentNo2indexDict_ == null)
                agentNo2indexDict_ = new Dictionary<int, int>();
            else
                agentNo2indexDict_.Clear();

            if (obstacles_ == null)
                obstacles_ = new List<Obstacle>();
            else
            {
                RecycleObstacles(obstacles_);
                obstacles_.Clear();
            }

            defaultAgent_ = null;
            if (kdTree_ == null)
                kdTree_ = new KdTree(this);

            nextAgentId_ = 0;
            ++agentVersion_;
            //globalTime_ = LFloat.zero;
            timeStep_ = LFloat.FromRaw(LFloat.Precision / 10);

        }

        /// <summary>断开障碍双向链表并把节点归还对象池，避免池中对象继续引用整条轮廓。</summary>
        private static void RecycleObstacles(List<Obstacle> obstacles)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];
                obstacle.next_ = null;
                obstacle.previous_ = null;
                obstacle.direction_ = LVector2.zero;
                obstacle.point_ = LVector2.zero;
                obstacle.id_ = 0;
                obstacle.convex_ = false;
                StaticPool.Set(obstacle);
            }
        }

        internal Dictionary<int, int> agentNo2indexDict_;
        internal List<Agent> agents_;
        internal List<Obstacle> obstacles_;
        internal KdTree kdTree_;
        internal int agentVersion_;
        internal LFloat invTimeStep_;
        private LFloat timeStepValue_;
        private int nextAgentId_;

        /// <summary>
        /// 每次 <see cref="doStep"/> 推进的固定时间。
        /// 必须大于零；赋值时同步缓存倒数，供已发生碰撞时的即时避让约束使用。
        /// </summary>
        public LFloat timeStep_
        {
            get => timeStepValue_;
            set
            {
                if (value <= LFloat.zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "RVO time step must be greater than zero.");
                timeStepValue_ = value;
                invTimeStep_ = LFloat.one / value;
            }
        }
        //public LFloat globalTime_ { get; private set; }
        private Agent defaultAgent_;
        //private ManualResetEvent[] doneEvents_;
        //private int numWorkers_;
        //private int workerAgentCount_;
        /// <summary>当前仍在模拟器列表中的代理数量。</summary>
        public int NumAgents => agents_.Count;

        /// <summary>障碍顶点数量；每个顶点同时代表从自身到下一顶点的一条有向边。</summary>
        public int NumObstacleVertices => obstacles_.Count;

        /// <summary>
        /// 标记指定代理待删除。实际压缩列表和重建编号映射发生在下一次 <see cref="doStep"/> 开头。
        /// </summary>
        public void delAgent(int agentNo) => agents_[agentNo2indexDict_[agentNo]].needDelete_ = true;


        /// <summary>
        /// 使用 <see cref="setAgentDefaults"/> 配置的参数创建代理。
        /// 尚未设置默认参数时返回 <see langword="null"/>。
        /// </summary>
        public Agent addAgent(LVector2 position)
        {
            if (defaultAgent_ == null)
                return default;
            return addAgent(position,
                defaultAgent_.neighborDist_,
                defaultAgent_.maxNeighbors_,
                defaultAgent_.timeHorizon_,
                defaultAgent_.timeHorizonObst_,
                defaultAgent_.radius_,
                defaultAgent_.maxSpeed_,
                defaultAgent_.velocity_);
        }


        /// <summary>
        /// 使用完整参数创建代理，并分配不会因列表压缩而改变的递增编号。
        /// <paramref name="timeHorizon"/> 与 <paramref name="timeHorizonObst"/> 必须由调用方保证为正数。
        /// </summary>
        public Agent addAgent(LVector2 position, LFloat neighborDist, int maxNeighbors, LFloat timeHorizon, LFloat timeHorizonObst, 
            LFloat radius, LFloat maxSpeed, LVector2 velocity)
        {
            Agent agent = new Agent(this, maxNeighbors);
            agent.id_ = nextAgentId_++;
            agent.maxNeighbors_ = maxNeighbors;
            agent.maxSpeed_ = maxSpeed;
            agent.neighborDist_ = neighborDist;
            agent.position_ = position;
            agent.radius_ = radius;
            agent.timeHorizon_ = timeHorizon;
            agent.timeHorizonObst_ = timeHorizonObst;
            agent.velocity_ = velocity;
            agents_.Add(agent);
            onAddAgent();
            ++agentVersion_;
            return agent;
        }

        /// <summary>
        /// 添加一条静态障碍轮廓，并返回首顶点编号。
        /// <para>顶点按输入顺序连接为循环双向链表；首尾重复点会被忽略，相邻重复点或不足两个有效点时返回 -1。</para>
        /// <para>两个顶点表示双面线段；三个及以上顶点按逆时针轮廓计算凸角标记。
        /// 添加后只标记障碍 KD 树失效，真正重建会延迟到查询或步进时。</para>
        /// </summary>
        public int addObstacle(IList<LVector2> vertices)
        {
            if (vertices == null)
            {
                return -1;
            }

            int vertexCount = vertices.Count;
            if (vertexCount > 2 && vertices[0] == vertices[vertexCount - 1])
                --vertexCount;
            if (vertexCount < 2)
                return -1;

            for (int i = 0; i < vertexCount; ++i)
            {
                int next = i == vertexCount - 1 ? 0 : i + 1;
                if (vertices[i] == vertices[next])
                    return -1;
            }

            int obstacleNo = obstacles_.Count;

            for (int i = 0; i < vertexCount; ++i)
            {
                Obstacle obstacle = StaticPool.Get<Obstacle>();
                obstacle.next_ = null;
                obstacle.previous_ = null;
                obstacle.point_ = vertices[i];

                if (i != 0)
                {
                    obstacle.previous_ = obstacles_[obstacles_.Count - 1];
                    obstacle.previous_.next_ = obstacle;
                }

                if (i == vertexCount - 1)
                {
                    obstacle.next_ = obstacles_[obstacleNo];
                    obstacle.next_.previous_ = obstacle;
                }

                obstacle.direction_ = RVOMath.normalize(vertices[(i == vertexCount - 1 ? 0 : i + 1)] - vertices[i]);

                if (vertexCount == 2)
                {
                    obstacle.convex_ = true;
                }
                else
                {
                    obstacle.convex_ = (RVOMath.leftOf(vertices[(i == 0 ? vertexCount - 1 : i - 1)], vertices[i], vertices[(i == vertexCount - 1 ? 0 : i + 1)]) >= LFloat.zero);
                }

                obstacle.id_ = obstacles_.Count;
                obstacles_.Add(obstacle);
            }

            kdTree_.invalidateObstacleTree();

            return obstacleNo;
        }


        /// <summary>代理列表压缩后，重新建立稳定编号到当前数组下标的映射。</summary>
        void onDelAgent()
        {
            agentNo2indexDict_.Clear();

            for (int i = 0; i < agents_.Count; i++)
            {
                int agentNo = agents_[i].id_;
                agentNo2indexDict_.Add(agentNo, i);
            }
        }

        /// <summary>把刚追加代理的稳定编号登记到下标映射。</summary>
        void onAddAgent()
        {
            if (agents_.Count == 0)
                return;

            int index = agents_.Count - 1;
            int agentNo = agents_[index].id_;
            agentNo2indexDict_.Add(agentNo, index);
        }
        /// <summary>
        /// 使用原地双指针压缩移除待删除代理，避免逐项删除造成反复搬移。
        /// 发生变化时递增版本号，使代理 KD 树在下一次使用前重建。
        /// </summary>
        void updateDeleteAgent()
        {
            int writeIndex = 0;
            int originalCount = agents_.Count;

            for (int readIndex = 0; readIndex < originalCount; ++readIndex)
            {
                Agent agent = agents_[readIndex];
                if (agent.needDelete_)
                {
                    continue;
                }

                if (writeIndex != readIndex)
                    agents_[writeIndex] = agent;
                ++writeIndex;
            }

            if (writeIndex != originalCount)
            {
                agents_.RemoveRange(writeIndex, originalCount - writeIndex);
                onDelAgent();
                ++agentVersion_;
            }
        }

        /// <summary>仅清空代理及其 KD 树，保留静态障碍和默认代理参数。</summary>
        public void ClearAgents()
        {
            bool hadAgents = agents_.Count > 0;
            agents_.Clear();
            agentNo2indexDict_.Clear();
            nextAgentId_ = 0;
            kdTree_.clearAgentTree();
            if (hadAgents)
                ++agentVersion_;
        }

        /// <summary>
        /// 推进一个固定模拟步。
        /// <para>先处理延迟删除并准备两类 KD 树，再让所有代理基于同一帧旧状态计算新速度，
        /// 最后统一提交速度和位置，从而保证结果不依赖代理遍历顺序。</para>
        /// </summary>
        public void doStep()
        {
            updateDeleteAgent();
            kdTree_.ensureObstacleTree();
            kdTree_.buildAgentTree();

            for (int i = 0; i < agents_.Count; i++)
            {
                var agent = agents_[i];
                agent.computeNeighbors();
                agent.computeNewVelocity();
            }
       
            for (int i = 0; i < agents_.Count; i++)
            {
                var agent = agents_[i];
                agent.update();
            }
   

            //globalTime_ += timeStep_;

            //return globalTime_;
        }


        /// <summary>按稳定代理编号取得代理，而不是按当前列表下标访问。</summary>
        public Agent getAgent(int agentNo) => agents_[agentNo2indexDict_[agentNo]];
        /// <summary>按顶点编号取得障碍节点；节点到 <see cref="Obstacle.next_"/> 构成一条障碍边。</summary>
        public Obstacle GetObstacle(int vertexNo) => obstacles_[vertexNo];




   




        /// <summary>立即确保障碍 KD 树已构建；通常可让首次步进或查询按需完成。</summary>
        public void processObstacles() => kdTree_.ensureObstacleTree();

        /// <summary>
        /// 判断两个点之间是否存在为给定半径留出净空的直线路径。
        /// 半径为零时退化为线段与障碍的可见性检测。
        /// </summary>
        public bool queryVisibility(LVector2 point1, LVector2 point2, LFloat radius)
        {
            kdTree_.ensureObstacleTree();
            return kdTree_.queryVisibility(point1, point2, radius);
        }
        /// <summary>查询半径内最近代理的稳定编号；范围内不存在代理时返回 -1。</summary>
        public int queryNearAgent(LVector2 point, LFloat radius)
        {
            if (NumAgents == 0)
                return -1;

            kdTree_.buildAgentTree();
            return kdTree_.queryNearAgent(point, radius);
        }
        /// <summary>
        /// 设置简化版 <see cref="addAgent(LVector2)"/> 使用的默认参数。
        /// 浮点输入会在此处统一转换为定点数；该设置只影响之后创建的代理。
        /// </summary>
        public void setAgentDefaults(float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst,
            float radius, float maxSpeed, LVector2 velocity)
        {
            if (defaultAgent_ == null)
            {
                defaultAgent_ = new Agent(this);
            }

            defaultAgent_.maxNeighbors_ = maxNeighbors;
            defaultAgent_.maxSpeed_ = maxSpeed.ToLFloat();
            defaultAgent_.neighborDist_ = neighborDist.ToLFloat();
            defaultAgent_.radius_ = radius.ToLFloat();
            defaultAgent_.timeHorizon_ = timeHorizon.ToLFloat();
            defaultAgent_.timeHorizonObst_ = timeHorizonObst.ToLFloat();
            defaultAgent_.velocity_ = velocity;
        }



    }

    /// <summary>
    /// RVO 查询使用的二维 KD 树集合。
    /// <para>代理位置每步都可能变化，因此代理树按代理版本和当前位置重建；静态障碍树仅在新增障碍时失效，
    /// 并在下一次步进或可见性查询时按需重建。</para>
    /// </summary>
    internal class KdTree
    {
        /// <summary>代理树节点；保存连续代理区间、子节点下标及区间包围盒。</summary>
        private struct AgentTreeNode
        {
            internal int begin_;
            internal int end_;
            internal int left_;
            internal int right_;
            internal LFloat maxX_;
            internal LFloat maxY_;
            internal LFloat minX_;
            internal LFloat minY_;
        }

        /// <summary>障碍 BSP/KD 节点；分割边把其余有向障碍边划分到左右子树。</summary>
        private class ObstacleTreeNode
        {
            internal Obstacle obstacle_;
            internal ObstacleTreeNode left_;
            internal ObstacleTreeNode right_;

            public ObstacleTreeNode()
            {
            }
        };

        /// <summary>代理树叶节点直接遍历的最大代理数量。</summary>
        private const int MAX_LEAF_SIZE = 10;

        private Agent[] agents_;
        private AgentTreeNode[] agentTree_;
        private int agentCount_;
        private int agentVersion_ = -1;
        private ObstacleTreeNode obstacleTree_;
        private bool obstacleTreeDirty_;
        private Simulator simulator;

        /// <summary>比较障碍分割的平衡程度：优先减小较大子树，其次减小较小子树。</summary>
        private static bool IsSplitWorseOrEqual(int left, int right, int bestLeft, int bestRight)
        {
            int largest = left > right ? left : right;
            int smallest = left < right ? left : right;
            int bestLargest = bestLeft > bestRight ? bestLeft : bestRight;
            int bestSmallest = bestLeft < bestRight ? bestLeft : bestRight;
            return largest > bestLargest || largest == bestLargest && smallest >= bestSmallest;
        }

        internal bool HasObstacleTree => obstacleTree_ != null;

        public KdTree(Simulator simulator)
        {
            this.simulator = simulator;
        }

        /// <summary>释放障碍树并清空代理树缓存，使该索引恢复到初始状态。</summary>
        internal void reset()
        {
            invalidateObstacleTree();
            obstacleTreeDirty_ = false;

            clearAgentTree();
        }

        /// <summary>清除代理数组中的对象引用和版本信息，便于代理对象被回收。</summary>
        internal void clearAgentTree()
        {
            if (agents_ != null && agentCount_ > 0)
                Array.Clear(agents_, 0, agentCount_);
            agentCount_ = 0;
            agentVersion_ = -1;
        }

        /// <summary>
        /// 按当前代理列表重建空间树。代理版本未变时仍会重建节点划分，因为位置每帧都会移动；
        /// 版本号用于决定是否必须从模拟器列表重新复制代理引用。
        /// </summary>
        internal void buildAgentTree()
        {
            int count = simulator.agents_.Count;

            if (count == 0)
            {
                if (agents_ != null && agentCount_ > 0)
                    Array.Clear(agents_, 0, agentCount_);
                agentCount_ = 0;
                agentVersion_ = simulator.agentVersion_;
                return;
            }

            if (agents_ == null || agents_.Length < count)
            {
                int capacity = agents_ == null || agents_.Length == 0 ? 16 : agents_.Length;
                while (capacity < count)
                    capacity *= 2;

                agents_ = new Agent[capacity];
                agentTree_ = new AgentTreeNode[2 * capacity];
                agentVersion_ = -1;
            }

            if (agentVersion_ != simulator.agentVersion_)
            {
                for (int i = 0; i < count; ++i)
                {
                    agents_[i] = simulator.agents_[i];
                }

                if (agentCount_ > count)
                    Array.Clear(agents_, count, agentCount_ - count);

                agentVersion_ = simulator.agentVersion_;
            }

            agentCount_ = count;
            buildAgentTreeRecursive(0, count, 0);
        }

        /// <summary>
        /// 重新构建静态障碍树。构建过程可能在障碍边跨越分割线时插入新的分割顶点，
        /// 因而模拟器的障碍顶点数量可能增加，但轮廓几何形状保持不变。
        /// </summary>
        internal void buildObstacleTree()
        {
            if (obstacleTree_ != null)
            {
                releaseObstacleTree(obstacleTree_);
                obstacleTree_ = null;
            }

            List<Obstacle> obstacles = StaticPool.Get<List<Obstacle>>();
            obstacles.Clear();
            if (obstacles.Capacity < simulator.obstacles_.Count)
                obstacles.Capacity = simulator.obstacles_.Count;
            try
            {
                for (int i = 0; i < simulator.obstacles_.Count; ++i)
                    obstacles.Add(simulator.obstacles_[i]);

                obstacleTree_ = buildObstacleTreeRecursive(obstacles);
                obstacleTreeDirty_ = false;
            }
            finally
            {
                obstacles.Clear();
                StaticPool.Set(obstacles);
            }
        }

        /// <summary>释放现有障碍树并标记为脏，使后续查询触发按需重建。</summary>
        internal void invalidateObstacleTree()
        {
            if (obstacleTree_ != null)
                releaseObstacleTree(obstacleTree_);
            obstacleTree_ = null;
            obstacleTreeDirty_ = true;
        }

        /// <summary>后序遍历释放障碍树节点，并清空引用后归还静态对象池。</summary>
        private static void releaseObstacleTree(ObstacleTreeNode node)
        {
            if (node == null) return;

            releaseObstacleTree(node.left_);
            releaseObstacleTree(node.right_);
            node.obstacle_ = null;
            node.left_ = null;
            node.right_ = null;
            StaticPool.Set(node);
        }

        /// <summary>仅在障碍数据发生过变化时重建障碍树。</summary>
        internal void ensureObstacleTree()
        {
            if (obstacleTreeDirty_)
                buildObstacleTree();
        }

        /// <summary>通过代理树查找范围内邻居；搜索半径会随候选列表填满而动态收紧。</summary>
        internal void computeAgentNeighbors(Agent agent, ref LFloat rangeSq)
        {
            queryAgentTreeRecursive(agent, ref rangeSq, 0);
        }

        /// <summary>通过障碍树收集代理右侧、且距离平方小于给定范围的障碍边。</summary>
        internal void computeObstacleNeighbors(Agent agent, LFloat rangeSq)
        {
            queryObstacleTreeRecursive(agent, rangeSq, obstacleTree_);
        }

        /// <summary>使用障碍树递归判断带半径线段是否可见。</summary>
        internal bool queryVisibility(LVector2 q1, LVector2 q2, LFloat radius)
        {
            return queryVisibilityRecursive(q1, q2, RVOMath.sqr(radius), obstacleTree_);
        }

        /// <summary>通过包围盒距离剪枝，查找给定半径内最近代理。</summary>
        internal int queryNearAgent(LVector2 point, LFloat radius)
        {
            LFloat rangeSq = RVOMath.sqr(radius);
            int agentNo = -1;
            queryAgentTreeRecursive(point, ref rangeSq, ref agentNo, 0);
            return agentNo;
        }

        /// <summary>
        /// 原地划分代理数组并构建子树。每次选择包围盒跨度更大的轴，在中点处分割；
        /// 子节点采用隐式数组下标，避免为每个节点分配对象。
        /// </summary>
        private void buildAgentTreeRecursive(int begin, int end, int node)
        {
            agentTree_[node].begin_ = begin;
            agentTree_[node].end_ = end;
            agentTree_[node].minX_ = agentTree_[node].maxX_ = agents_[begin].position_.x;
            agentTree_[node].minY_ = agentTree_[node].maxY_ = agents_[begin].position_.y;

            for (int i = begin + 1; i < end; ++i)
            {
                agentTree_[node].maxX_ = LMath.Max(agentTree_[node].maxX_, agents_[i].position_.x);
                agentTree_[node].minX_ = LMath.Min(agentTree_[node].minX_, agents_[i].position_.x);
                agentTree_[node].maxY_ = LMath.Max(agentTree_[node].maxY_, agents_[i].position_.y);
                agentTree_[node].minY_ = LMath.Min(agentTree_[node].minY_, agents_[i].position_.y);
            }

            if (end - begin > MAX_LEAF_SIZE)
            {
                bool isVertical = agentTree_[node].maxX_ - agentTree_[node].minX_ > agentTree_[node].maxY_ - agentTree_[node].minY_;
                LFloat splitValue = LFloat.half * (isVertical ? agentTree_[node].maxX_ + agentTree_[node].minX_ : agentTree_[node].maxY_ + agentTree_[node].minY_);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? agents_[left].position_.x : agents_[left].position_.y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? agents_[right - 1].position_.x : agents_[right - 1].position_.y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        Agent tempAgent = agents_[left];
                        agents_[left] = agents_[right - 1];
                        agents_[right - 1] = tempAgent;
                        ++left;
                        --right;
                    }
                }

                int leftSize = left - begin;

                if (leftSize == 0)
                {
                    ++leftSize;
                    ++left;
                    ++right;
                }

                agentTree_[node].left_ = node + 1;
                agentTree_[node].right_ = node + 2 * leftSize;

                buildAgentTreeRecursive(begin, left, agentTree_[node].left_);
                buildAgentTreeRecursive(left, end, agentTree_[node].right_);
            }
        }

        /// <summary>
        /// 递归构建以有向障碍边为分割面的 BSP 树。
        /// <para>候选分割边以左右子集尽量平衡为准；跨越分割线的边会在交点处切成两段，
        /// 新顶点同时接入原障碍双向链表，保证后续速度障碍计算仍能取得正确前后顶点。</para>
        /// </summary>
        private ObstacleTreeNode buildObstacleTreeRecursive(List<Obstacle> obstacles)
        {
            if (obstacles.Count == 0)
            {
                return null;
            }

            ObstacleTreeNode node = StaticPool.Get<ObstacleTreeNode>();
            node.obstacle_ = null;
            node.left_ = null;
            node.right_ = null;

            int optimalSplit = 0;
            int minLeft = obstacles.Count;
            int minRight = obstacles.Count;

            for (int i = 0; i < obstacles.Count; ++i)
            {
                int leftSize = 0;
                int rightSize = 0;

                Obstacle obstacleI1 = obstacles[i];
                Obstacle obstacleI2 = obstacleI1.next_;

                for (int j = 0; j < obstacles.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Obstacle obstacleJ1 = obstacles[j];
                    Obstacle obstacleJ2 = obstacleJ1.next_;

                    LFloat j1LeftOfI = RVOMath.leftOf(obstacleI1.point_, obstacleI2.point_, obstacleJ1.point_);
                    LFloat j2LeftOfI = RVOMath.leftOf(obstacleI1.point_, obstacleI2.point_, obstacleJ2.point_);

                    if (j1LeftOfI >= -RVOMath.RVO_EPSILON && j2LeftOfI >= -RVOMath.RVO_EPSILON)
                    {
                        ++leftSize;
                    }
                    else if (j1LeftOfI <= RVOMath.RVO_EPSILON && j2LeftOfI <= RVOMath.RVO_EPSILON)
                    {
                        ++rightSize;
                    }
                    else
                    {
                        ++leftSize;
                        ++rightSize;
                    }

                    if (IsSplitWorseOrEqual(leftSize, rightSize, minLeft, minRight))
                    {
                        break;
                    }
                }

                if (!IsSplitWorseOrEqual(leftSize, rightSize, minLeft, minRight))
                {
                    minLeft = leftSize;
                    minRight = rightSize;
                    optimalSplit = i;
                }
            }

            {
                // 临时左右集合来自对象池，递归结束后立即清空归还，降低构树时的 GC 压力。
                List<Obstacle> leftObstacles = StaticPool.Get<List<Obstacle>>();
                List<Obstacle> rightObstacles = StaticPool.Get<List<Obstacle>>();
                try
                {
                    leftObstacles.Clear();
                    rightObstacles.Clear();
                    if (leftObstacles.Capacity < minLeft)
                        leftObstacles.Capacity = minLeft;
                    if (rightObstacles.Capacity < minRight)
                        rightObstacles.Capacity = minRight;

                    int i = optimalSplit;

                    Obstacle obstacleI1 = obstacles[i];
                    Obstacle obstacleI2 = obstacleI1.next_;

                    for (int j = 0; j < obstacles.Count; ++j)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        Obstacle obstacleJ1 = obstacles[j];
                        Obstacle obstacleJ2 = obstacleJ1.next_;

                        LFloat j1LeftOfI = RVOMath.leftOf(obstacleI1.point_, obstacleI2.point_, obstacleJ1.point_);
                        LFloat j2LeftOfI = RVOMath.leftOf(obstacleI1.point_, obstacleI2.point_, obstacleJ2.point_);

                        if (j1LeftOfI >= -RVOMath.RVO_EPSILON && j2LeftOfI >= -RVOMath.RVO_EPSILON)
                        {
                            leftObstacles.Add(obstacles[j]);
                        }
                        else if (j1LeftOfI <= RVOMath.RVO_EPSILON && j2LeftOfI <= RVOMath.RVO_EPSILON)
                        {
                            rightObstacles.Add(obstacles[j]);
                        }
                        else
                        {
                            // 跨越分割线的障碍边必须切开，否则该边无法被唯一归入任一侧子树。
                            LFloat t = RVOMath.det(obstacleI2.point_ - obstacleI1.point_, obstacleJ1.point_ - obstacleI1.point_) / RVOMath.det(obstacleI2.point_ - obstacleI1.point_, obstacleJ1.point_ - obstacleJ2.point_);

                            LVector2 splitPoint = obstacleJ1.point_ + t * (obstacleJ2.point_ - obstacleJ1.point_);

                            Obstacle newObstacle = StaticPool.Get<Obstacle>();
                            newObstacle.point_ = splitPoint;
                            newObstacle.previous_ = obstacleJ1;
                            newObstacle.next_ = obstacleJ2;
                            newObstacle.convex_ = true;
                            newObstacle.direction_ = obstacleJ1.direction_;

                            newObstacle.id_ = simulator.obstacles_.Count;

                            simulator.obstacles_.Add(newObstacle);

                            obstacleJ1.next_ = newObstacle;
                            obstacleJ2.previous_ = newObstacle;

                            if (j1LeftOfI > LFloat.zero)
                            {
                                leftObstacles.Add(obstacleJ1);
                                rightObstacles.Add(newObstacle);
                            }
                            else
                            {
                                rightObstacles.Add(obstacleJ1);
                                leftObstacles.Add(newObstacle);
                            }
                        }
                    }

                    node.obstacle_ = obstacleI1;
                    node.left_ = buildObstacleTreeRecursive(leftObstacles);
                    node.right_ = buildObstacleTreeRecursive(rightObstacles);

                    return node;
                }
                finally
                {
                    leftObstacles.Clear();
                    rightObstacles.Clear();
                    StaticPool.Set(leftObstacles);
                    StaticPool.Set(rightObstacles);
                }
            }
        }

        /// <summary>
        /// 查询离指定点最近的代理。先访问包围盒更近的子树，找到候选后缩小范围，
        /// 从而尽可能剪掉另一侧子树。
        /// </summary>
        private void queryAgentTreeRecursive(LVector2 position, ref LFloat rangeSq, ref int agentNo, int node)
        {
            if (agentTree_[node].end_ - agentTree_[node].begin_ <= MAX_LEAF_SIZE)
            {
                for (int i = agentTree_[node].begin_; i < agentTree_[node].end_; ++i)
                {
                    LFloat distSq = RVOMath.absSq(position - agents_[i].position_);
                    if (distSq < rangeSq)
                    {
                        rangeSq = distSq;
                        agentNo = agents_[i].id_;
                    }
                }
            }
            else
            {
                LFloat distSqLeft = RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].left_].minX_ - position.x)) + RVOMath.sqr(LMath.Max(LFloat.zero, position.x - agentTree_[agentTree_[node].left_].maxX_)) + RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].left_].minY_ - position.y)) + RVOMath.sqr(LMath.Max(LFloat.zero, position.y - agentTree_[agentTree_[node].left_].maxY_));
                LFloat distSqRight = RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].right_].minX_ - position.x)) + RVOMath.sqr(LMath.Max(LFloat.zero, position.x - agentTree_[agentTree_[node].right_].maxX_)) + RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].right_].minY_ - position.y)) + RVOMath.sqr(LMath.Max(LFloat.zero, position.y - agentTree_[agentTree_[node].right_].maxY_));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        queryAgentTreeRecursive(position, ref rangeSq, ref agentNo, agentTree_[node].left_);

                        if (distSqRight < rangeSq)
                        {
                            queryAgentTreeRecursive(position, ref rangeSq, ref agentNo, agentTree_[node].right_);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        queryAgentTreeRecursive(position, ref rangeSq, ref agentNo, agentTree_[node].right_);

                        if (distSqLeft < rangeSq)
                        {
                            queryAgentTreeRecursive(position, ref rangeSq, ref agentNo, agentTree_[node].left_);
                        }
                    }
                }
            }
        }

        /// <summary>查询指定代理的近邻，并把叶节点候选交给代理执行有序、限量插入。</summary>
        private void queryAgentTreeRecursive(Agent agent, ref LFloat rangeSq, int node)
        {
            if (agentTree_[node].end_ - agentTree_[node].begin_ <= MAX_LEAF_SIZE)
            {
                for (int i = agentTree_[node].begin_; i < agentTree_[node].end_; ++i)
                {
                    agent.insertAgentNeighbor(agents_[i], ref rangeSq);
                }
            }
            else
            {
                LFloat distSqLeft = RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].left_].minX_ - agent.position_.x)) + RVOMath.sqr(LMath.Max(LFloat.zero, agent.position_.x - agentTree_[agentTree_[node].left_].maxX_)) + RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].left_].minY_ - agent.position_.y)) + RVOMath.sqr(LMath.Max(LFloat.zero, agent.position_.y - agentTree_[agentTree_[node].left_].maxY_));
                LFloat distSqRight = RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].right_].minX_ - agent.position_.x)) + RVOMath.sqr(LMath.Max(LFloat.zero, agent.position_.x - agentTree_[agentTree_[node].right_].maxX_)) + RVOMath.sqr(LMath.Max(LFloat.zero, agentTree_[agentTree_[node].right_].minY_ - agent.position_.y)) + RVOMath.sqr(LMath.Max(LFloat.zero, agent.position_.y - agentTree_[agentTree_[node].right_].maxY_));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].left_);

                        if (distSqRight < rangeSq)
                        {
                            queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].right_);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].right_);

                        if (distSqLeft < rangeSq)
                        {
                            queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].left_);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查询影响代理的障碍边。优先访问代理所在侧；仅当代理到分割直线的距离小于查询半径时，
        /// 才检查分割边及另一侧子树。
        /// </summary>
        private void queryObstacleTreeRecursive(Agent agent, LFloat rangeSq, ObstacleTreeNode node)
        {
            if (node != null)
            {
                Obstacle obstacle1 = node.obstacle_;
                Obstacle obstacle2 = obstacle1.next_;

                LFloat agentLeftOfLine = RVOMath.leftOf(obstacle1.point_, obstacle2.point_, agent.position_);

                queryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= LFloat.zero ? node.left_ : node.right_);

                LFloat distSqLine = RVOMath.sqr(agentLeftOfLine) / RVOMath.absSq(obstacle2.point_ - obstacle1.point_);

                if (distSqLine < rangeSq)
                {
                    if (agentLeftOfLine < LFloat.zero)
                    {
                        agent.insertObstacleNeighbor(node.obstacle_, rangeSq);
                    }

                    queryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= LFloat.zero ? node.right_ : node.left_);
                }
            }
        }

        /// <summary>
        /// 递归判断带净空半径的查询线段是否穿过障碍。
        /// 根据查询端点相对分割边的朝向选择必查子树，并用端点到直线的距离决定是否需要检查另一侧。
        /// </summary>
        private bool queryVisibilityRecursive(LVector2 q1, LVector2 q2, LFloat radiusSq, ObstacleTreeNode node)
        {
            if (node == null)
            {
                return true;
            }

            Obstacle obstacle1 = node.obstacle_;
            Obstacle obstacle2 = obstacle1.next_;

            LFloat q1LeftOfI = RVOMath.leftOf(obstacle1.point_, obstacle2.point_, q1);
            LFloat q2LeftOfI = RVOMath.leftOf(obstacle1.point_, obstacle2.point_, q2);
            LFloat invLengthI = LFloat.one / RVOMath.absSq(obstacle2.point_ - obstacle1.point_);

            if (q1LeftOfI >= LFloat.zero && q2LeftOfI >= LFloat.zero)
            {
                return queryVisibilityRecursive(q1, q2, radiusSq, node.left_) && ((RVOMath.sqr(q1LeftOfI) * invLengthI >= radiusSq && RVOMath.sqr(q2LeftOfI) * invLengthI >= radiusSq) || queryVisibilityRecursive(q1, q2, radiusSq, node.right_));
            }

            if (q1LeftOfI <= LFloat.zero && q2LeftOfI <= LFloat.zero)
            {
                return queryVisibilityRecursive(q1, q2, radiusSq, node.right_) && ((RVOMath.sqr(q1LeftOfI) * invLengthI >= radiusSq && RVOMath.sqr(q2LeftOfI) * invLengthI >= radiusSq) || queryVisibilityRecursive(q1, q2, radiusSq, node.left_));
            }

            if (q1LeftOfI >= LFloat.zero && q2LeftOfI <= LFloat.zero)
            {
                return queryVisibilityRecursive(q1, q2, radiusSq, node.left_) && queryVisibilityRecursive(q1, q2, radiusSq, node.right_);
            }

            LFloat point1LeftOfQ = RVOMath.leftOf(q1, q2, obstacle1.point_);
            LFloat point2LeftOfQ = RVOMath.leftOf(q1, q2, obstacle2.point_);
            LFloat invLengthQ = LFloat.one / RVOMath.absSq(q2 - q1);

            return point1LeftOfQ * point2LeftOfQ >= LFloat.zero && RVOMath.sqr(point1LeftOfQ) * invLengthQ > radiusSq && RVOMath.sqr(point2LeftOfQ) * invLengthQ > radiusSq && queryVisibilityRecursive(q1, q2, radiusSq, node.left_) && queryVisibilityRecursive(q1, q2, radiusSq, node.right_);
        }
    }

    /// <summary>
    /// 速度空间中的 ORCA 约束线。
    /// <see cref="point"/> 是线上一点，<see cref="direction"/> 是单位切向；
    /// 按该方向观察时，直线左侧为满足约束的速度半平面。
    /// </summary>
    public struct Line
    {
        /// <summary>约束线的单位切向。</summary>
        public LVector2 direction;
        /// <summary>约束线经过的速度空间点。</summary>
        public LVector2 point;
    }

    /// <summary>
    /// 静态障碍轮廓中的一个顶点，同时代表从本顶点指向 <see cref="next_"/> 的有向边。
    /// 同一轮廓的节点通过 <see cref="next_"/> 和 <see cref="previous_"/> 组成循环双向链表。
    /// </summary>
    public class Obstacle
    {
        /// <summary>轮廓中的下一顶点。</summary>
        public Obstacle next_;
        /// <summary>轮廓中的上一顶点。</summary>
        public Obstacle previous_;
        /// <summary>从当前点到下一点的单位方向。</summary>
        public LVector2 direction_;
        /// <summary>当前障碍顶点坐标。</summary>
        public LVector2 point_;
        /// <summary>障碍顶点在模拟器中的稳定编号。</summary>
        public int id_;
        /// <summary>当前顶点是否为凸角；用于决定速度障碍的左右切线。</summary>
        public bool convex_;
    }

    /// <summary>RVO 算法使用的二维定点几何辅助函数。</summary>
    public struct RVOMath
    {
        /// <summary>用于平行、退化线段和边界判断的最小定点容差。</summary>
        internal static readonly LFloat RVO_EPSILON = LFloat.FromRaw(1L);

        /// <summary>返回向量长度。</summary>
        public static LFloat abs(LVector2 vector)
        {
            return LMath.Sqrt(absSq(vector));
        }

        /// <summary>返回向量长度平方，适合只做距离比较时避免开平方。</summary>
        public static LFloat absSq(LVector2 vector)
        {
            return LVector2.Dot(vector, vector);
        }

        /// <summary>返回单位向量；零向量保持为零，避免除零。</summary>
        public static LVector2 normalize(LVector2 vector)
        {
            LFloat length = abs(vector);
            if (length > LFloat.zero)
                return vector / length;
            return vector;
        }

        /// <summary>返回二维行列式（叉积标量），其符号用于判断左右关系。</summary>
        internal static LFloat det(LVector2 vector1, LVector2 vector2)
        {
            return LVector2.Cross(vector1, vector2);
        }

        /// <summary>
        /// 返回点 <paramref name="vector3"/> 到线段
        /// [<paramref name="vector1"/>, <paramref name="vector2"/>] 的最短距离平方。
        /// 退化线段按到首端点的距离处理。
        /// </summary>
        internal static LFloat distSqPointLineSegment(LVector2 vector1, LVector2 vector2, LVector2 vector3)
        {
            LVector2 segment = vector2 - vector1;
            LVector2 pointOffset = vector3 - vector1;
            LFloat segmentLengthSq = absSq(segment);
            if (segmentLengthSq <= RVO_EPSILON)
                return absSq(pointOffset);

            LFloat r = LVector2.Dot(pointOffset, segment) / segmentLengthSq;

            if (r < LFloat.zero)
            {
                return absSq(pointOffset);
            }

            if (r > LFloat.one)
            {
                return absSq(vector3 - vector2);
            }

            return absSq(vector3 - (vector1 + r * segment));
        }

        /// <summary>
        /// 返回点 <paramref name="c"/> 相对有向直线 a→b 的有符号面积；
        /// 正值在左侧，负值在右侧，零表示共线。
        /// </summary>
        internal static LFloat leftOf(LVector2 a, LVector2 b, LVector2 c)
        {
            return det(a - c, b - a);
        }

        /// <summary>返回标量平方。</summary>
        internal static LFloat sqr(LFloat scalar)
        {
            return scalar * scalar;
        }
    }
}
