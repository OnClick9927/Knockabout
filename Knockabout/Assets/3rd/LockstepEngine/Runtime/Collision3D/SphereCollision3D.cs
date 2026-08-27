namespace Lockstep.Collision
{
    /// <summary>
    /// 三维球形碰撞体。pos 为球心，_radius 为未缩放半径，公开 radius 会乘统一绝对缩放。
    /// 球体不受 rotation 影响。
    /// </summary>
    public class SphereCollision3D : Collision3D<SphereCollision3D>
    {
        private LFloat _radius;

        public LFloat radius => _radius * AbsScale(scale);

        public static SphereCollision3D New(LVector3 pos, LFloat radius)
        {
            var sphere = New();
            sphere._radius = LMath.Abs(radius);
            sphere.Init(pos, LQuaternion.identity, LFloat.one);
            return sphere;
        }

        public override bool SetRadius(LFloat radius)
        {
            radius = LMath.Abs(radius);
            if (_radius == radius) return false;
            _radius = radius;
            return true;
        }

        public override bool SetSize(LVector3 size) => false;

        /// <summary>用世界半径在三个轴上扩张球心，得到精确 AABB。</summary>
        public override void CalcBounds()
        {
            var extents = LVector3.one * radius;
            bounds = new LBounds(pos - extents, pos + extents);
        }
    }
}
