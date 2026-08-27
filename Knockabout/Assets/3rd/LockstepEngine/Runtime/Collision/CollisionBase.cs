using System.Collections.Generic;

namespace Lockstep.Collision
{
    /// <summary>
    /// 二维与三维碰撞形状共享的最小生命周期基类。
    /// <para>这里只保存与空间维度无关的统一缩放，并定义包围盒刷新和对象池回收契约。
    /// 位置、旋转、包围盒类型和窄相位算法仍由二维、三维派生类分别实现。</para>
    /// </summary>
    public abstract class CollisionBase
    {
        private LFloat scaleValue;

        /// <summary>形状当前使用的统一缩放值。</summary>
        public LFloat scale => scaleValue;

        /// <summary>
        /// 初始化池化形状的缩放值，不执行变更比较。
        /// 派生类可通过 <see cref="NormalizeScale"/> 规定本维度的缩放约束。
        /// </summary>
        protected void InitScale(LFloat scale)
        {
            scaleValue = NormalizeScale(scale);
        }

        /// <summary>
        /// 修改统一缩放，并返回归一化后的值是否发生变化。
        /// 该方法只更新权威变换数据，包围盒由代理所属的树在 Update 阶段集中刷新。
        /// </summary>
        protected bool SetScaleValue(LFloat scale)
        {
            scale = NormalizeScale(scale);
            if (scaleValue == scale) return false;

            scaleValue = scale;
            return true;
        }

        /// <summary>
        /// 把外部缩放转换为派生维度接受的值。
        /// 二维默认保留符号以维持原行为，三维会重写为绝对值。
        /// </summary>
        protected virtual LFloat NormalizeScale(LFloat scale) => scale;

        /// <summary>从全局静态池取得指定的具体碰撞形状。</summary>
        protected static TCollision GetFromPool<TCollision>()
            where TCollision : CollisionBase, new()
        {
            return StaticPool.Get<TCollision>();
        }

        /// <summary>
        /// 把当前形状按具体类型归还全局静态池。
        /// 自引用泛型约束出现错误时记录日志，避免把对象放入错误的池。
        /// </summary>
        protected void ReturnToPool<TCollision>()
            where TCollision : CollisionBase, new()
        {
            if (this is TCollision value)
                StaticPool.Set(value);
            else
                Debug.LogError($"{GetType()} cannot be recycled as {typeof(TCollision)}.");
        }

        /// <summary>根据当前权威变换和形状尺寸重新计算宽相位包围盒。</summary>
        public abstract void CalcBounds();

        /// <summary>清理形状持有的外部引用，并把实例归还对应对象池。</summary>
        public abstract void Cycle();
    }

    /// <summary>
    /// 二维与三维碰撞代理共享的池化生命周期和树成员状态。
    /// <para>代理负责把业务对象、碰撞层和具体形状绑定在一起；维度相关的位置、旋转、
    /// 包围盒及脏标记由派生代理暴露和维护。</para>
    /// </summary>
    /// <typeparam name="TAgent">最终代理类型，用于无反射地访问对应静态对象池。</typeparam>
    /// <typeparam name="TCollision">该代理持有的维度碰撞形状基类。</typeparam>
    public abstract class CollisionAgentBase<TAgent, TCollision>
        where TAgent : CollisionAgentBase<TAgent, TCollision>, new()
        where TCollision : CollisionBase
    {
        private TCollision collisionValue;

        /// <summary>调用方挂接的业务数据；回收代理时会清空。</summary>
        public object userData { get; private set; }

        /// <summary>代理持有的具体维度碰撞形状。</summary>
        public TCollision collision => collisionValue;

        /// <summary>直接转发形状的统一缩放。</summary>
        public LFloat scale => collisionValue.scale;

        /// <summary>用于查询过滤的碰撞层。</summary>
        public CollisionLayer layer { get; private set; }

        /// <summary>
        /// 代理在所属树稳定列表中的下标；-1 表示尚未加入任何树。
        /// 该字段由共用树基类登记和移除辅助方法统一维护。
        /// </summary>
        internal int treeIndex = -1;

        /// <summary>从代理池取得实例并完整覆盖上一次使用遗留的公共与维度状态。</summary>
        protected static TAgent Create(
            TCollision collision,
            CollisionLayer layer,
            object userData)
        {
            TAgent agent = StaticPool.Get<TAgent>();
            agent.ResetDimensionState();
            agent.treeIndex = -1;
            agent.collisionValue = collision;
            agent.userData = userData;
            agent.layer = layer;
            return agent;
        }

        /// <summary>
        /// 回收形状并清空代理状态，最后把代理归还其具体类型的静态池。
        /// 树必须先解除成员关系，确保回收时 <see cref="treeIndex"/> 已恢复为 -1。
        /// </summary>
        internal void Cycle()
        {
            collisionValue?.Cycle();
            collisionValue = null;
            userData = null;
            layer = default;
            treeIndex = -1;
            ResetDimensionState();
            StaticPool.Set((TAgent)this);
        }

        /// <summary>清除节点引用、变换脏标记等仅属于具体维度的代理状态。</summary>
        protected abstract void ResetDimensionState();
    }

    /// <summary>
    /// 二维与三维碰撞树共享的代理列表和成员维护基类。
    /// <para>空间索引、包围盒更新和几何查询由派生树实现；本类只统一不会因维度变化的
    /// 层过滤、成员校验、下标登记及两种移除顺序策略。</para>
    /// </summary>
    public abstract class CollisionTreeBase<TAgent, TCollision>
        where TAgent : CollisionAgentBase<TAgent, TCollision>, new()
        where TCollision : CollisionBase
    {
        /// <summary>树当前持有的全部代理；treeIndex 始终指向代理在此列表中的位置。</summary>
        protected readonly List<TAgent> agents = new List<TAgent>();

        /// <summary>树中当前代理数量。</summary>
        public int count => agents.Count;

        /// <summary>判断代理的碰撞层是否包含在可选过滤列表中；空列表表示接受全部层。</summary>
        protected static bool IsLayerMatch(TAgent agent, int[] layers)
        {
            if (layers == null || layers.Length == 0) return true;

            int layerValue = agent.layer.value;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layerValue)
                    return true;
            }
            return false;
        }

        /// <summary>检查代理的 treeIndex 是否与当前树列表一致。</summary>
        protected bool IsRegistered(TAgent agent)
        {
            return agent != null &&
                   agent.treeIndex >= 0 &&
                   agent.treeIndex < agents.Count &&
                   ReferenceEquals(agents[agent.treeIndex], agent);
        }

        /// <summary>把已经完成维度索引插入的代理追加到公共列表，并设置 treeIndex。</summary>
        protected void RegisterAgent(TAgent agent)
        {
            agent.treeIndex = agents.Count;
            agents.Add(agent);
        }

        /// <summary>
        /// 使用末项回填方式 O(1) 移除代理。
        /// 适用于二维四叉树，因为其同距离结果本来就不依赖全局插入顺序。
        /// </summary>
        protected bool UnregisterAgentSwapBack(TAgent agent)
        {
            if (!IsRegistered(agent)) return false;

            int index = agent.treeIndex;
            int lastIndex = agents.Count - 1;
            if (index != lastIndex)
            {
                TAgent last = agents[lastIndex];
                agents[index] = last;
                last.treeIndex = index;
            }

            agents.RemoveAt(lastIndex);
            agent.treeIndex = -1;
            return true;
        }

        /// <summary>
        /// 保持其余代理相对顺序地移除指定代理，并刷新受影响的 treeIndex。
        /// 三维线性宽相位以 treeIndex 打破同距离平局，因此需要保留该顺序。
        /// </summary>
        protected bool UnregisterAgentOrdered(TAgent agent)
        {
            if (!IsRegistered(agent)) return false;

            int index = agent.treeIndex;
            agents.RemoveAt(index);
            agent.treeIndex = -1;
            for (int i = index; i < agents.Count; i++)
                agents[i].treeIndex = i;
            return true;
        }

        /// <summary>刷新所有脏代理的包围盒及派生空间索引。</summary>
        public abstract void Update();
    }
}
