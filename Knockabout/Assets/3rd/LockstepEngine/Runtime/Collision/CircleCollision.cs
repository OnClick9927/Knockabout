namespace Lockstep.Collision
{
    /// <summary>
    /// 二维圆形碰撞体。位置为圆心，radius 为未缩放半径，最终半径还会乘以统一 scale。
    /// 实例来自 StaticPool，移出碰撞树时由代理统一回收。
    /// </summary>
    public class CircleCollision : Collision<CircleCollision>
    {
        /// <summary>创建指定圆心与半径的池化圆形碰撞体。</summary>
        public static CircleCollision New(LVector2 pos, LFloat radius)
        {
            CircleCollision circle = New();
            circle.Init(pos, radius, LFloat.zero);

            return circle;
        }


        public override bool SetRadius(LFloat size)
        {
            return Set_Radius(size);
        }
        public override bool SetSize(LVector2 size) => false;

    }
}
