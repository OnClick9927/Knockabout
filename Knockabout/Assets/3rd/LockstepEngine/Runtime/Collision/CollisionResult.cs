
namespace Lockstep.Collision
{
    /// <summary>
    /// 二维重叠查询的一条结果，包含命中的代理、接触法线和用于近远排序的距离。
    /// 法线方向遵循具体窄相位函数的 A/B 参数顺序。
    /// </summary>
    public struct CollisionResult
    {
        public CollisionResult(CollisionAgent agent, LVector2 normal, LFloat dis)
        {
            this.agent = agent;
            this.normal = normal;
            this.dis = dis;
        }

        public CollisionAgent agent { get; private set; }
        public LVector2 normal { get; private set; }
        public LFloat dis { get; private set; }

    }

}
