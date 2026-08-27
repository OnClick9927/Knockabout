
namespace Lockstep.Collision
{
    /// <summary>
    /// 二维射线的单个命中结果。o/d 保存查询射线，hitPoint/normal 保存表面信息，
    /// dis 是起点到交点的实际距离；CollisionTree 会按 dis 从近到远排序。
    /// </summary>
    public struct RayCastResult
    {
        /// <summary>命中的代理，可通过 userData 找回业务对象。</summary>
        public CollisionAgent agent { get; private set; }
        public LVector2 o { get; private set; }
        public LVector2 d { get; private set; }
        public LFloat dis { get; private set; }
        public LVector2 hitPoint { get; private set; }
        public LVector2 normal { get; private set; }
        public RayCastResult(LVector2 hitPoint, CollisionAgent agent, LVector2 o, LVector2 d, LVector2 normal)
        {
            this.dis = (hitPoint-o).magnitude;
            this.agent = agent;
            this.o = o;
            this.d = d;
            this.hitPoint = hitPoint;
            this.normal = normal;
        }


    }

}
