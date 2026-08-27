namespace Lockstep.Collision
{
    /// <summary>
    /// 二维有向包围盒。_size 是未缩放完整尺寸，deg 为角度制旋转，
    /// CalcBounds 会把旋转后的四个方向投影到世界轴，生成供四叉树使用的 AABB。
    /// </summary>
    public class OBBCollision : Collision<OBBCollision>
    {
        public LVector2 _size;
        public LVector2 size => _size * scale;
        public static OBBCollision New(LVector2 pos, LVector2 size, LFloat deg)
        {
            var obb = New();
            obb.Init(pos, size, deg);
            return obb;
        }

        private void Init(LVector2 pos, LVector2 size, LFloat deg) 
        {
            this._size = size;
            base.Init(pos, size.magnitude / 2, deg);
        }

        public override bool SetRadius(LFloat size) => false;
        public override bool SetSize(LVector2 size)
        {
            if (this._size == size) return false;
            this._size = size;
            Set_Radius(size.magnitude/2);
            return true;
        }

        /// <summary>根据当前朝向计算能够完整包住 OBB 的世界空间轴对齐包围盒。</summary>
        public override void CalcBounds()
        {
            var halfSize = size / 2;
            var right = new LVector2(up.y, -up.x);
            var halfWidth = LMath.Abs(right.x) * halfSize.x + LMath.Abs(up.x) * halfSize.y;
            var halfHeight = LMath.Abs(right.y) * halfSize.x + LMath.Abs(up.y) * halfSize.y;
            bounds = LRect.CreateRect(pos, new LVector2(halfWidth, halfHeight));
        }
    }

}
