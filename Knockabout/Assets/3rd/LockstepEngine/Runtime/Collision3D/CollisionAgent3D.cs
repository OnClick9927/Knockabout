namespace Lockstep.Collision
{
    /// <summary>
    /// 三维形状在 CollisionTree3D 中的池化代理。
    /// userData 关联业务对象，treeIndex 记录稳定树序号，任何有效变换或尺寸修改都会设置
    /// BoundsChanged，由树在下一次 Update 中集中调用 CalcBounds。
    /// </summary>
    public class CollisionAgent3D : CollisionAgentBase<CollisionAgent3D, Collision3D>
    {
        /// <summary>从静态池取得代理，并清除上次使用留下的树归属和脏标记。</summary>
        internal static CollisionAgent3D New(
            Collision3D collision,
            CollisionLayer layer,
            object userData)
        {
            return Create(collision, layer, userData);
        }

        /// <summary>清除三维包围盒脏标记。</summary>
        protected override void ResetDimensionState()
        {
            BoundsChanged = false;
        }

        internal bool BoundsChanged;

        public LBounds bounds => collision.bounds;
        public LVector3 pos => collision.pos;
        public LQuaternion rotation => collision.rotation;

        /// <summary>设置世界位置；值实际变化时标记包围盒失效。</summary>
        public void SetPos(LVector3 pos)
        {
            if (pos == this.pos) return;
            collision.SetPos(pos);
            BoundsChanged = true;
        }

        public void SetRotation(LQuaternion rotation)
        {
            if (rotation == this.rotation) return;
            collision.SetRotation(rotation);
            BoundsChanged = true;
        }

        public void Rotate(LQuaternion rotation)
        {
            collision.Rotate(rotation);
            BoundsChanged = true;
        }

        public void SetScale(LFloat scale)
        {
            if (scale == this.scale) return;
            collision.SetScale(scale);
            BoundsChanged = true;
        }

        public void SetRadius(LFloat radius)
        {
            collision.SetRadius(radius);
            BoundsChanged = true;
        }

        public void SetSize(LVector3 size)
        {
            collision.SetSize(size);
            BoundsChanged = true;
        }

        public void SetHeight(LFloat height)
        {
            collision.SetHeight(height);
            BoundsChanged = true;
        }
    }
}
