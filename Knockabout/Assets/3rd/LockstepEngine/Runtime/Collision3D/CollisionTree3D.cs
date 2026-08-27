using System;
using System.Collections.Generic;

namespace Lockstep.Collision
{
    /// <summary>
    /// 三维碰撞代理容器与查询入口。
    /// 当前实现以稳定线性列表作为宽相位，并用每个形状的 AABB 做快速剔除；
    /// Update 集中刷新脏包围盒。重叠、射线和最近邻都支持层与业务过滤器。
    /// </summary>
    public class CollisionTree3D : CollisionTreeBase<CollisionAgent3D, Collision3D>
    {
        /// <summary>校验代理归属，计算初始 AABB，并分配稳定 treeIndex。</summary>
        public void Add(CollisionAgent3D agent)
        {
            if (agent == null)
            {
                Debug.LogError("Cannot add a null collision agent.");
                return;
            }
            if (agent.collision == null)
            {
                Debug.LogError("Cannot add a collision agent without a collision.");
                return;
            }
            if (agent.treeIndex >= 0)
            {
                Debug.LogError("Cannot add a collision agent that is already in a tree.");
                return;
            }

            agent.collision.CalcBounds();
            agent.BoundsChanged = false;
            RegisterAgent(agent);
        }

        private bool RemoveWithoutCycle(CollisionAgent3D agent)
        {
            return UnregisterAgentOrdered(agent);
        }

        public void Remove(CollisionAgent3D agent)
        {
            if (!RemoveWithoutCycle(agent))
            {
                Debug.LogError("Cannot remove a collision agent that is not in this tree.");
                return;
            }
            agent.Cycle();
        }

        /// <summary>回收树中所有代理及其形状，然后清空稳定列表。</summary>
        public void Clear()
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                CollisionAgent3D agent = agents[i];
                agent.treeIndex = -1;
                agent.Cycle();
            }
            agents.Clear();
        }

        /// <summary>只为 BoundsChanged 的代理重算包围盒，并清除脏标记。</summary>
        public override void Update()
        {
            for (int i = 0; i < agents.Count; i++)
            {
                CollisionAgent3D agent = agents[i];
                if (!agent.BoundsChanged) continue;

                agent.collision.CalcBounds();
                agent.BoundsChanged = false;
            }
        }

        /// <summary>
        /// 查询与指定形状重叠的全部代理；跳过形状自身，并按距离和 treeIndex 稳定排序。
        /// </summary>
        public List<CollisionResult3D> OverLap(
            Collision3D collision,
            List<CollisionResult3D> result,
            Func<CollisionAgent3D, bool> fit = null,
            params int[] layers)
        {
            result = result ?? new List<CollisionResult3D>();
            result.Clear();
            if (collision == null) return result;

            for (int i = 0; i < agents.Count; i++)
            {
                CollisionAgent3D agent = agents[i];
                Collision3D candidate = agent.collision;
                if (ReferenceEquals(candidate, collision)) continue;
                if (!IsLayerMatch(agent, layers)) continue;
                if (fit != null && !fit(agent)) continue;
                if (!collision.bounds.Overlaps(candidate.bounds)) continue;

                if (collision.OverLap(candidate, out CollisionContact3D contact))
                {
                    LFloat distance = (collision.pos - contact.pointB).magnitude;
                    result.Add(new CollisionResult3D(agent, contact, distance));
                }
            }

            if (result.Count > 1)
            {
                result.Sort((left, right) =>
                {
                    int compare = left.dis.CompareTo(right.dis);
                    return compare != 0
                        ? compare
                        : left.agent.treeIndex.CompareTo(right.agent.treeIndex);
                });
            }
            return result;
        }

        /// <summary>
        /// 发射一条无限长射线，并返回从近到远排列的全部命中结果。
        /// </summary>
        public List<RayCastResult3D> RayCast(
            LVector3 origin,
            LVector3 direction,
            List<RayCastResult3D> result,
            Func<CollisionAgent3D, bool> fit = null,
            params int[] layers)
        {
            return RayCast(
                origin, direction, LFloat.MaxValue, result, fit, layers);
        }

        /// <summary>
        /// 发射一条有限长度射线，并返回从近到远排列的全部命中结果。
        /// </summary>
        /// <param name="origin">射线的世界空间起点。</param>
        /// <param name="direction">
        /// 射线方向，长度不会影响检测结果；查询前会被归一化。
        /// </param>
        /// <param name="maxDistance">最大检测距离，小于等于零时不执行查询。</param>
        /// <param name="result">
        /// 用于复用的结果列表。传入 null 时会创建新列表；每次查询前都会清空。
        /// </param>
        /// <param name="fit">可选的业务过滤器，返回 false 的代理不会参与检测。</param>
        /// <param name="layers">可选的碰撞层编号；不传表示检测全部层。</param>
        public List<RayCastResult3D> RayCast(
            LVector3 origin,
            LVector3 direction,
            LFloat maxDistance,
            List<RayCastResult3D> result,
            Func<CollisionAgent3D, bool> fit = null,
            params int[] layers)
        {
            result = result ?? new List<RayCastResult3D>();
            result.Clear();

            var normalizedDirection = direction.normalized;
            if (normalizedDirection == LVector3.zero || maxDistance <= LFloat.zero)
                return result;

            // CollisionTree3D 当前使用线性代理表。这里仍先检测代理的 AABB，
            // 让未与射线相交的复杂网格跳过后续逐三角形窄相位检测。
            for (int i = 0; i < agents.Count; i++)
            {
                CollisionAgent3D agent = agents[i];
                if (!IsLayerMatch(agent, layers)) continue;
                if (fit != null && !fit(agent)) continue;
                if (!CollisionTools3D.TestRayBounds(
                    origin, normalizedDirection, agent.bounds, maxDistance)) continue;

                LVector3 hitPoint;
                LVector3 normal;
                int feature;
                // 方向已在树入口归一化，直接进入窄相位，避免定点向量重复归一化。
                if (!CollisionTools3D.TestRay(
                    agent.collision, origin, normalizedDirection,
                    out hitPoint, out normal, out feature)) continue;

                var hit = new RayCastResult3D(
                    hitPoint, agent, origin, normalizedDirection, normal, feature);
                if (hit.dis <= maxDistance)
                    result.Add(hit);
            }

            // 距离相同时用代理在树中的稳定序号打破平局，保证锁步端排序一致。
            if (result.Count > 1)
            {
                result.Sort((left, right) =>
                {
                    int compare = left.dis.CompareTo(right.dis);
                    return compare != 0
                        ? compare
                        : left.agent.treeIndex.CompareTo(right.agent.treeIndex);
                });
            }
            return result;
        }

        /// <summary>
        /// 按代理中心距离平方查找最近对象。dis 既是输入上限也是输出距离平方。
        /// </summary>
        public CollisionAgent3D Nearest(
            LVector3 point,
            ref LFloat dis,
            Func<CollisionAgent3D, bool> fit = null,
            params int[] layers)
        {
            CollisionAgent3D result = null;
            for (int i = 0; i < agents.Count; i++)
            {
                CollisionAgent3D agent = agents[i];
                if (!IsLayerMatch(agent, layers)) continue;
                if (fit != null && !fit(agent)) continue;

                LFloat distanceSquared = (agent.pos - point).sqrMagnitude;
                if (distanceSquared >= dis) continue;

                dis = distanceSquared;
                result = agent;
            }
            return result;
        }
    }
}
