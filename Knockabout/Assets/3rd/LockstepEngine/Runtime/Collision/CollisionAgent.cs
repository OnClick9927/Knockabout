namespace Lockstep.Collision
{
    /// <summary>
    /// 二维碰撞形状在 CollisionTree 中的身份与可变状态。
    /// 位置变化设置 Moved，尺寸/角度/缩放变化设置 RadiusChanged；Tree.Update
    /// 会统一刷新包围盒并在必要时把代理迁移到新的四叉树节点。
    /// </summary>
    public class CollisionAgent : CollisionAgentBase<CollisionAgent, Collision>
    {
        /// <summary>从静态池取得代理并重置所有树归属和脏标记。</summary>
        internal static CollisionAgent New(Collision collision, CollisionLayer layer, object userData)
        {
            return Create(collision, layer, userData);
        }

        /// <summary>清除二维四叉树节点引用以及位置、包围盒脏标记。</summary>
        protected override void ResetDimensionState()
        {
            node = null;
            Moved = false;
            RadiusChanged = false;
        }

        internal CollisionNode node;

        public LRect bounds => collision.bounds;
        public LVector2 pos => collision.pos;
        public LFloat deg => collision.deg;
        internal bool Moved = false;
        internal bool RadiusChanged = false;

        /// <summary>更新位置并标记代理可能需要迁移节点。</summary>
        public void SetPos(LVector2 pos)
        {
            if (pos == this.pos) return;
            collision.SetPos(pos);
            Moved = true;
        }
        public void Rotate(LFloat deg)
        {
            if (collision.SetDeg(this.deg + deg))
                RadiusChanged = true;
        }
        public void SetDeg(LFloat deg)
        {
            if (collision.SetDeg(deg))
                RadiusChanged = true;
        }

        public void SetScale(LFloat scale)
        {
            if (scale == this.scale) return;

            collision.SetScale(scale);
            RadiusChanged = true;
        }

        public void SetRadius(LFloat size)
        {
            if (collision.SetRadius(size))
                RadiusChanged = true;
        }
        public void SetSize(LVector2 size)
        {
            if (collision.SetSize(size))
                RadiusChanged = true;
        }


    }

}
