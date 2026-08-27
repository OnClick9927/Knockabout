namespace Lockstep.Collision
{
    /// <summary>
    /// 三维胶囊体，由轴线段 pointA-pointB 与半径组成。
    /// height 表示包含两端半球的总高度，最小值为直径；rotation 决定局部 Y 轴朝向。
    /// </summary>
    public class CapsuleCollision3D : Collision3D<CapsuleCollision3D>
    {
        private LFloat _radius;
        private LFloat _height;

        public LFloat radius => _radius * AbsScale(scale);
        public LFloat height => _height * AbsScale(scale);
        public LVector3 axis => rotation * LVector3.up;
        public LVector3 pointA => pos - axis * SegmentHalfLength;
        public LVector3 pointB => pos + axis * SegmentHalfLength;

        /// <summary>从胶囊中心到任一半球球心的距离。</summary>
        private LFloat SegmentHalfLength => LMath.Max(height / 2 - radius, LFloat.zero);

        public static CapsuleCollision3D New(LVector3 pos, LFloat radius, LFloat height)
        {
            return New(pos, radius, height, LQuaternion.identity);
        }

        public static CapsuleCollision3D New(
            LVector3 pos,
            LFloat radius,
            LFloat height,
            LQuaternion rotation)
        {
            var capsule = New();
            capsule._radius = LMath.Abs(radius);
            capsule._height = LMath.Max(LMath.Abs(height), capsule._radius * 2);
            capsule.Init(pos, rotation, LFloat.one);
            return capsule;
        }

        public override bool SetRadius(LFloat radius)
        {
            radius = LMath.Abs(radius);
            if (_radius == radius) return false;
            _radius = radius;
            _height = LMath.Max(_height, _radius * 2);
            return true;
        }

        public override bool SetHeight(LFloat height)
        {
            height = LMath.Max(LMath.Abs(height), _radius * 2);
            if (_height == height) return false;
            _height = height;
            return true;
        }

        public override bool SetSize(LVector3 size) => false;

        /// <summary>先包住任意朝向的轴线段，再在三个轴上扩张胶囊半径。</summary>
        public override void CalcBounds()
        {
            var a = pointA;
            var b = pointB;
            var radius = this.radius;
            var extents = LVector3.one * radius;
            var min = new LVector3(
                LMath.Min(a.x, b.x),
                LMath.Min(a.y, b.y),
                LMath.Min(a.z, b.z)) - extents;
            var max = new LVector3(
                LMath.Max(a.x, b.x),
                LMath.Max(a.y, b.y),
                LMath.Max(a.z, b.z)) + extents;
            bounds = new LBounds(min, max);
        }
    }
}
