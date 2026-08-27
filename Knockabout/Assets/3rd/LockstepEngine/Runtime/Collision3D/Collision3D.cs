namespace Lockstep.Collision
{
    /// <summary>为具体三维形状提供静态池创建和强类型回收。</summary>
    public abstract class Collision3D<T> : Collision3D where T : Collision3D, new()
    {
        protected static T New() => GetFromPool<T>();

        public override void Cycle()
        {
            ReturnToPool<T>();
        }
    }

    /// <summary>
    /// 三维碰撞形状公共基类。
    /// 保存世界位置、单位旋转、统一缩放和最新 AABB；具体形状负责尺寸语义与 CalcBounds。
    /// 变换 Setter 只修改权威数据，代理会通过 BoundsChanged 通知树在 Update 中刷新包围盒。
    /// </summary>
    public abstract class Collision3D : CollisionBase
    {
        public LVector3 pos { get; private set; }
        public LQuaternion rotation { get; private set; }
        public LBounds bounds { get; protected set; }

        /// <summary>创建携带碰撞层与业务数据的池化三维代理。</summary>
        public CollisionAgent3D MakeAgent(CollisionLayer layer, object userData = null)
        {
            return CollisionAgent3D.New(this, layer, userData);
        }

        /// <summary>初始化池化形状，归一化旋转、取缩放绝对值并立即计算 AABB。</summary>
        protected void Init(LVector3 pos, LQuaternion rotation, LFloat scale)
        {
            this.pos = pos;
            this.rotation = Normalize(rotation);
            InitScale(scale);
            CalcBounds();
        }

        public bool SetPos(LVector3 pos)
        {
            if (this.pos == pos) return false;
            this.pos = pos;
            return true;
        }

        public bool SetRotation(LQuaternion rotation)
        {
            rotation = Normalize(rotation);
            if (this.rotation == rotation) return false;
            this.rotation = rotation;
            return true;
        }

        public bool Rotate(LQuaternion rotation)
        {
            return SetRotation(rotation * this.rotation);
        }

        public bool SetScale(LFloat scale)
        {
            return SetScaleValue(scale);
        }

        /// <summary>按统一缩放、旋转、平移顺序把局部点转换到世界空间。</summary>
        public LVector3 TransformPoint(LVector3 point)
        {
            return pos + rotation * (point * scale);
        }

        /// <summary>执行世界点到局部空间的逆变换；零缩放退化时返回零向量。</summary>
        public LVector3 InverseTransformPoint(LVector3 point)
        {
            if (scale == LFloat.zero) return LVector3.zero;
            return (LQuaternion.Inverse(rotation) * (point - pos)) / scale;
        }

        /// <summary>先做双方 AABB 宽相位，再进入具体形状组合的窄相位。</summary>
        public bool OverLap(Collision3D collision, out CollisionContact3D contact)
        {
            contact = default;
            if (collision == null || !bounds.Overlaps(collision.bounds)) return false;
            return CollisionTools3D.Test(this, collision, out contact);
        }

        /// <summary>
        /// 检测一条无限长射线是否命中当前形状。
        /// </summary>
        /// <param name="origin">射线的世界空间起点。</param>
        /// <param name="direction">
        /// 射线方向，允许传入非单位向量；方法内部会统一归一化。
        /// </param>
        /// <param name="hitPoint">命中时返回世界空间交点。</param>
        /// <param name="normal">命中时返回交点处的单位法线。</param>
        /// <param name="feature">
        /// 命中的子特征编号；网格使用三角形序号，基础形状为 -1。
        /// </param>
        public bool RayCast(
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal,
            out int feature)
        {
            hitPoint = LVector3.zero;
            normal = LVector3.zero;
            feature = -1;

            var normalizedDirection = direction.normalized;
            if (normalizedDirection == LVector3.zero) return false;

            return CollisionTools3D.TestRay(
                this, origin, normalizedDirection, out hitPoint, out normal, out feature);
        }

        public abstract bool SetRadius(LFloat radius);
        public abstract bool SetSize(LVector3 size);
        public virtual bool SetHeight(LFloat height) => false;
        public abstract override void CalcBounds();
        public abstract override void Cycle();

        protected static LFloat AbsScale(LFloat scale) => LMath.Abs(scale);

        /// <summary>三维尺寸统一使用非负缩放，负输入按绝对值处理。</summary>
        protected override LFloat NormalizeScale(LFloat scale) => LMath.Abs(scale);

        private static LQuaternion Normalize(LQuaternion value)
        {
            var lengthSquared = LQuaternion.Dot(value, value);
            if (lengthSquared <= LFloat.EPSILON)
                return LQuaternion.identity;

            var length = LMath.Sqrt(lengthSquared);
            return new LQuaternion(
                value.x / length,
                value.y / length,
                value.z / length,
                value.w / length);
        }
    }
}
