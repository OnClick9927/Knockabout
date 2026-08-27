
using System;
using System.Collections.Generic;


namespace Lockstep.Collision
{
    /// <summary>
    /// CollisionTree 的四叉树节点。
    /// 未拆分节点直接持有代理；超过容量且尺寸允许时拆成四个子节点。
    /// bounds 除了节点固定 rect，还会包住内部代理，用于查询时提前剪枝。
    /// </summary>
    class CollisionNode
    {
        public LRect rect;
        public LRect bounds;
        public CollisionNode parent { get; set; }
        /// <summary>自底向上重算当前子树的动态包围盒。</summary>
        public LRect CalcBounds()
        {
            bounds = rect;
            LFloat xMin, xMax, yMin, yMax;
            xMin = bounds.x;
            yMin = bounds.y;
            yMax = bounds.yMax;
            xMax = bounds.xMax;

            if (Splited)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    var rect = node.CalcBounds();
                    xMin = LMath.Min(xMin, rect.x);
                    yMin = LMath.Min(yMin, rect.y);
                    xMax = LMath.Max(xMax, rect.xMax);
                    yMax = LMath.Max(yMax, rect.yMax);
                }
            }
            else
            {
                if (agents.Count != 0)
                {
                    for (int i = 0; i < agents.Count; i++)
                    {
                        var agentBounds = agents[i].bounds;
                        xMin = LMath.Min(xMin, agentBounds.x);
                        yMin = LMath.Min(yMin, agentBounds.y);
                        xMax = LMath.Max(xMax, agentBounds.xMax);
                        yMax = LMath.Max(yMax, agentBounds.yMax);
                    }
                }

            }

            bounds.Set(xMin, yMin, xMax - xMin, yMax - yMin);
            return bounds;
        }

        public readonly List<CollisionAgent> agents = new List<CollisionAgent>();
        public readonly List<CollisionNode> nodes = new List<CollisionNode>();
        private int agentCount;
        private bool Splited;
        private CollisionTree tree;

        /// <summary>从静态池取得节点并完整重置上一次使用留下的层级状态。</summary>
        public static CollisionNode New(LRect rect, CollisionTree tree)
        {
            CollisionNode node = StaticPool.Get<CollisionNode>();
            node.bounds = LRect.zero;
            node.rect = rect;
            node.Splited = false;
            node.agentCount = 0;
            node.parent = null;
            node.agents.Clear();
            node.nodes.Clear();
            node.tree = tree;
            return node;
        }
        internal void Cycle()
        {
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].Cycle();

            bounds = LRect.zero;
            Splited = false;
            agentCount = 0;
            parent = null;
            tree = null;
            agents.Clear();
            nodes.Clear();
            StaticPool.Set(this);
        }
        public bool ContainsPoint(LVector2 pos) => rect.Contains(pos);
        public bool StillContains(CollisionAgent agent) => rect.Contains(agent.pos);
        private static bool IsLayerMatch(CollisionAgent agent, int[] layers)
        {
            if (layers == null || layers.Length == 0) return true;

            var layerValue = agent.layer.value;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layerValue)
                    return true;
            }
            return false;
        }

        /// <summary>使用子树 bounds 剪枝，收集满足层和业务过滤器的重叠代理。</summary>
        public List<CollisionResult> OverLap(Collision collision, Func<CollisionAgent, bool> fit, List<CollisionResult> result, int[] layers)
        {
            if (nodes.Count == 0 && agents.Count == 0) return result;

            if (!collision.bounds.Overlaps(this.bounds)) return result;
            if (Splited)
                for (int i = 0; i < nodes.Count; i++)
                    nodes[i].OverLap(collision, fit, result, layers);
            else
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    var agent = agents[i];
                    var data = agent.collision;
                    if (data == collision) continue;
                    if (!IsLayerMatch(agent, layers)) continue;
                    if (fit != null && !fit.Invoke(agent)) continue;

                    if (data.OverLap(collision, out var normal, out var point))
                    {
                        result.Add(new CollisionResult(agent, normal, (collision.pos - point).magnitude));
                    }
                }
            }
            return result;
        }
        /// <summary>先检测射线与节点 bounds，再递归收集具体形状命中。</summary>
        public List<RayCastResult> RayCast(LVector2 o, LVector2 d, Func<CollisionAgent, bool> fit, List<RayCastResult> result, int[] layers)
        {
            if (nodes.Count == 0 && agents.Count == 0) return result;

            LVector2 min = this.bounds.position; LVector2 max = this.bounds.max;
            if (!CollisionTools.TestRayAABB(o, d, min, max, out LVector2 _, out LVector2 __)) return result;
            if (Splited)
                for (int i = 0; i < nodes.Count; i++)
                    nodes[i].RayCast(o, d, fit, result, layers);
            else
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    var agent = agents[i];
                    if (!IsLayerMatch(agent, layers)) continue;
                    if (fit != null && !fit.Invoke(agent)) continue;
                    if (agent.collision.RayCast(o, d, out var hitPoint, out var normal))
                    {
                        result.Add(new RayCastResult(hitPoint, agent, o, d, normal));
                    }
                }
            }
            return result;
        }

        private static LFloat GetBoundsDistanceSquared(LRect rect, LVector2 point)
        {
            LFloat dx = point.x < rect.x
                ? rect.x - point.x
                : point.x > rect.xMax ? point.x - rect.xMax : LFloat.zero;
            LFloat dy = point.y < rect.y
                ? rect.y - point.y
                : point.y > rect.yMax ? point.y - rect.yMax : LFloat.zero;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 按包围盒最小距离剪枝搜索最近代理；dis 输入和输出均为距离平方。
        /// </summary>
        public void SearchNearest(LVector2 pos, ref LFloat dis, ref CollisionAgent result,
            Func<CollisionAgent, bool> fit, int[] layers)
        {
            if (agentCount == 0) return;
            var pointX = pos.x;
            var pointY = pos.y;
            var rect = this.bounds;
            // 若节点不包含目标点，且节点边界到目标点的距离大于当前最小距离，跳过
            if (!rect.Contains(pos))
            {
                var closestX = LMath.Max(rect.x, LMath.Min(pointX, rect.xMax));
                var closestY = LMath.Max(rect.y, LMath.Min(pointY, rect.yMax));
                var dx = pointX - closestX;
                var dy = pointY - closestY;
                if (dx * dx + dy * dy >= dis)
                    return;
            }
            if (Splited)
            {
                int visited = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    int nearestIndex = -1;
                    LFloat nearestDistance = LFloat.MaxValue;
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if ((visited & (1 << j)) != 0) continue;
                        if (nodes[j].agentCount == 0) continue;
                        LFloat distance = GetBoundsDistanceSquared(nodes[j].bounds, pos);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestIndex = j;
                        }
                    }

                    if (nearestIndex < 0 || nearestDistance >= dis)
                        break;
                    visited |= 1 << nearestIndex;
                    nodes[nearestIndex].SearchNearest(pos, ref dis, ref result, fit, layers);
                }
            }
            else
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    var agent = agents[i];
                    if (!IsLayerMatch(agent, layers)) continue;
                    if (fit != null && !fit.Invoke(agent)) continue;
                    var distanceSquared = (agent.pos - pos).sqrMagnitude;
                    if (distanceSquared < dis)
                    {
                        dis = distanceSquared;
                        result = agent;
                    }
                }

            }
        }



        public void SetChildrenParentAsThis()
        {
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].parent = this;
        }
        /// <summary>创建四个等分子节点，并把当前叶节点中的代理重新分配。</summary>
        private void Split()
        {
            var _rect = this.rect;
            LFloat xMin = _rect.x;
            LFloat yMin = _rect.y;
            LFloat halfWidth = _rect.width / 2;
            LFloat halfHeight = _rect.height / 2;
            //左上
            nodes.Add(New(new LRect(xMin, yMin, halfWidth, halfHeight), tree));
            //右上
            nodes.Add(New(new LRect(xMin + halfWidth, yMin, halfWidth, halfHeight), tree));
            //右下
            nodes.Add(New(new LRect(xMin + halfWidth, yMin + halfHeight, halfWidth, halfHeight), tree));

            nodes.Add(New(new LRect(xMin, yMin + halfHeight, halfWidth, halfHeight), tree));

            Splited = true;
        }
        /// <summary>收集所有后代代理并回收子节点，把当前节点退化回叶节点。</summary>
        private void UnSplit()
        {
            List<CollisionAgent> collected = StaticPool.Get<List<CollisionAgent>>();
            collected.Clear();
            try
            {
                CollectAgent(collected);
                for (int i = 0; i < collected.Count; i++)
                {
                    if (!ContainsPoint(collected[i].pos))
                        return;
                }

                agents.Clear();
                for (int i = 0; i < nodes.Count; i++)
                    nodes[i].Cycle();

                nodes.Clear();
                Splited = false;
                agentCount = 0;
                for (int i = 0; i < collected.Count; i++)
                    AddAgent(collected[i]);
            }
            finally
            {
                collected.Clear();
                StaticPool.Set(collected);
            }
        }

        internal int GetAgentCount()
        {
            return agentCount;
        }

        public void CollectAgent(List<CollisionAgent> result)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                result.Add(agent);
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.CollectAgent(result);
            }
        }



        private void AddToChild(CollisionAgent agent)
        {
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].AddAgent(agent);
        }
        public void AddAgent(CollisionAgent agent)
        {
            if (!ContainsPoint(agent.pos)) return;
            agentCount++;
            if (Splited)
                AddToChild(agent);
            else
            {
                var _rect = this.rect;
                if (agents.Count >= CollisionTree.AgentMaxCountPerCell && _rect.width > CollisionTree.CellMinSize * 2 && _rect.height > CollisionTree.CellMinSize * 2)
                {
                    Split();
                    SetChildrenParentAsThis();
                    for (int i = 0; i < agents.Count; i++)
                        AddToChild(agents[i]);
                    AddToChild(agent);
                    agents.Clear();
                }
                else
                {
                    agent.node = this;
                    agents.Add(agent);
                }
            }
        }

        public bool Remove(CollisionAgent agent)
        {
            CollisionNode leaf = agent?.node;
            if (leaf == null || !leaf.agents.Remove(agent))
                return false;

            agent.node = null;
            leaf.agentCount--;
            CollisionNode node = leaf.parent;
            while (node != null)
            {
                node.agentCount--;
                node = node.parent;
            }

            node = leaf.parent;
            while (node != null)
            {
                CollisionNode parent = node.parent;
                if (node.Splited && node.GetAgentCount() <= CollisionTree.AgentMaxCountPerCell)
                    node.UnSplit();
                node = parent;
            }

            return true;

        }

        internal void Read(List<CollisionNode> children)
        {
            this.Splited = true;
            this.nodes.Clear();
            this.nodes.AddRange(children);
            this.agentCount = 0;
            for (int i = 0; i < children.Count; i++)
                this.agentCount += children[i].agentCount;
        }

        internal void DrawGizmos()
        {
#if UNITY_5_3_OR_NEWER
            GizmosTools.DrawRect(this.rect, UnityEngine.Color.red, tree.type);
            //CollisonUtils.DrawRect(this.bounds, UnityEngine.Color.blue);
#endif
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].DrawGizmos();

        }


    }

}
