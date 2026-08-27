namespace Lockstep.Collision
{
    /// <summary>
    /// 三维有向包围盒。size 是完整边长，halfSize 是半边长；
    /// axisX/Y/Z 由定点四元数旋转世界基向量得到，供 SAT 和支撑点算法使用。
    /// </summary>
    public class BoxCollision3D : Collision3D<BoxCollision3D>
    {
        private LVector3 _size;

        public LVector3 size => _size * AbsScale(scale);
        public LVector3 halfSize => size / 2;
        public LVector3 axisX => rotation * LVector3.right;
        public LVector3 axisY => rotation * LVector3.up;
        public LVector3 axisZ => rotation * LVector3.forward;

        public static BoxCollision3D New(LVector3 pos, LVector3 size)
        {
            return New(pos, size, LQuaternion.identity);
        }

        public static BoxCollision3D New(LVector3 pos, LVector3 size, LQuaternion rotation)
        {
            var box = New();
            box._size = Abs(size);
            box.Init(pos, rotation, LFloat.one);
            return box;
        }

        public override bool SetRadius(LFloat radius) => false;

        public override bool SetSize(LVector3 size)
        {
            size = Abs(size);
            if (_size == size) return false;
            _size = size;
            return true;
        }

        /// <summary>
        /// 把三个局部半轴分别投影到世界 X/Y/Z，并累加绝对投影得到包围 OBB 的 AABB。
        /// </summary>
        public override void CalcBounds()
        {
            var half = halfSize;
            var x = axisX;
            var y = axisY;
            var z = axisZ;
            var extents = new LVector3(
                LMath.Abs(x.x) * half.x + LMath.Abs(y.x) * half.y + LMath.Abs(z.x) * half.z,
                LMath.Abs(x.y) * half.x + LMath.Abs(y.y) * half.y + LMath.Abs(z.y) * half.z,
                LMath.Abs(x.z) * half.x + LMath.Abs(y.z) * half.y + LMath.Abs(z.z) * half.z);
            bounds = new LBounds(pos - extents, pos + extents);
        }

        private static LVector3 Abs(LVector3 value)
        {
            return new LVector3(
                LMath.Abs(value.x),
                LMath.Abs(value.y),
                LMath.Abs(value.z));
        }
    }
}
