namespace Lockstep.Collision
{
    /// <summary>
    /// 一次 3D 重叠查询的结果。
    /// </summary>
    public struct CollisionResult3D
    {
        public CollisionResult3D(
            CollisionAgent3D agent,
            CollisionContact3D contact,
            LFloat dis)
        {
            this.agent = agent;
            this.contact = contact;
            this.dis = dis;
        }

        /// <summary>命中的树代理。</summary>
        public CollisionAgent3D agent { get; private set; }
        /// <summary>双方窄相位产生的接触数据。</summary>
        public CollisionContact3D contact { get; private set; }
        /// <summary>查询形状到接触点的距离，用于稳定排序。</summary>
        public LFloat dis { get; private set; }
    }

    /// <summary>
    /// 一次 3D 射线查询的单个命中结果。
    /// 射线树会按 <see cref="dis"/> 从近到远排列多个结果。
    /// </summary>
    public struct RayCastResult3D
    {
        public RayCastResult3D(
            LVector3 hitPoint,
            CollisionAgent3D agent,
            LVector3 origin,
            LVector3 direction,
            LVector3 normal,
            int feature = -1)
        {
            this.agent = agent;
            this.origin = origin;
            this.direction = direction;
            this.hitPoint = hitPoint;
            this.normal = normal;
            this.feature = feature;

            // direction 在查询入口已经归一化，但直接根据两点求距离更稳妥：
            // 即使未来结果由其他入口构造，dis 仍然是实际的世界空间距离。
            dis = (hitPoint - origin).magnitude;
        }

        /// <summary>被射线命中的碰撞代理，可通过 userData 找回业务对象。</summary>
        public CollisionAgent3D agent { get; private set; }

        /// <summary>射线起点。</summary>
        public LVector3 origin { get; private set; }

        /// <summary>归一化后的射线方向。</summary>
        public LVector3 direction { get; private set; }

        /// <summary>射线起点到命中点的世界空间距离。</summary>
        public LFloat dis { get; private set; }

        /// <summary>世界空间命中点。</summary>
        public LVector3 hitPoint { get; private set; }

        /// <summary>命中表面的世界空间单位法线。</summary>
        public LVector3 normal { get; private set; }

        /// <summary>
        /// 命中特征编号。网格返回三角形序号，其他形状返回 -1。
        /// </summary>
        public int feature { get; private set; }
    }
}
