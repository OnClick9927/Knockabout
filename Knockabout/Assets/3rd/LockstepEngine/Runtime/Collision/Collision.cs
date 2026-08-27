namespace Lockstep.Collision
{
    /// <summary>
    /// 为具体二维形状提供静态池创建与回收的泛型基类。
    /// </summary>
    public abstract class Collision<T> : Collision where T : Collision, new()
    {
        protected static T New() => GetFromPool<T>();

        public override void Cycle()
        {
            ReturnToPool<T>();
        }


    }

    /// <summary>
    /// 二维碰撞形状公共基类。
    /// pos/deg/scale 是权威变换，bounds 是四叉树宽相位使用的世界 AABB，
    /// radius 是覆盖形状的外接圆半径。形状只负责几何，层和业务数据保存在 CollisionAgent。
    /// </summary>
    public abstract class Collision : CollisionBase
    {
        /// <summary>为当前形状创建可加入 CollisionTree 的池化代理。</summary>
        public CollisionAgent MakeAgent(CollisionLayer layer, object userData = null)
        {
            return CollisionAgent.New(this, layer, userData);
        }


        private LFloat _radius;
        public LVector2 pos { get; private set; }
        public LRect bounds { get; protected set; }
        public LFloat deg { get; private set; }
        public LVector2 up { get; private set; }
        public LFloat radius => _radius * scale;
        //public CollisionLayer layer { get; set; }
        //public int layerValue => layer.value;

        public abstract override void Cycle();
        /// <summary>初始化池中复用的形状，并立即建立方向向量和包围盒。</summary>
        protected void Init(LVector2 pos, LFloat radius, LFloat deg)
        {
            Set_Radius(radius);
            SetDeg(deg);
            SetScale(LFloat.one);
            SetPos(pos);
            CalcBounds();

        }
        public void SetScale(LFloat scale) => SetScaleValue(scale);
        protected bool Set_Radius(LFloat value)
        {

            if (_radius == value) return false;
            _radius = value;
            return true;
        }

        public void Rotate(LFloat rdeg)
        {
            rdeg += deg;


            SetDeg(rdeg);
        }
        /// <summary>
        /// 设置角度制旋转并更新 up 方向；超出一圈的角度会先归约到 [-360, 360]。
        /// </summary>
        public bool SetDeg(LFloat rdeg)
        {
            if (this.deg == rdeg && up != LVector2.zero) return false;
            if (rdeg > 360 || rdeg < -360)
                rdeg = rdeg - (rdeg / 360 * 360);
            deg = rdeg;
            var rad = LMath.Deg2Rad * deg;
            var c = LMath.Cos(rad);
            var s = LMath.Sin(rad);
            up = new LVector2(s, c);
            return true;
            //up = new LVector2(c, s);
        }

        public void SetPos(LVector2 pos) => this.pos = pos;
        public abstract bool SetRadius(LFloat size);
        public abstract bool SetSize(LVector2 size);
        public override void CalcBounds() => bounds = new LRect(pos.x - radius, pos.y - radius, radius * 2, radius * 2);

        /// <summary>
        /// 先做 AABB 宽相位，再按双方运行时类型分派到对应窄相位算法。
        /// </summary>
        internal bool OverLap(Collision collision, out LVector2 normal, out LVector2 point)
        {
            point = normal = LVector2.zero;
            //if (!collision.layer.CouldCollison(layer)) return false;
            if (!collision.bounds.Overlaps(this.bounds)) return false;
            var posa = this.pos;
            var posb = collision.pos;
            LFloat ra = this.radius;
            LFloat rb = collision.radius;
            if (this is CircleCollision a)
            {
                if (collision is CircleCollision b)
                    return CollisionTools.TestCircleCircle(posa, ra, posb, rb, out normal, out point);
                else if (collision is OBBCollision rect)
                    return CollisionTools.TestCircleOBB(posa, ra, posb, rb, rect.size / 2, rect.up, out normal, out point);
                else if (collision is PolygonCollision p)
                {
                    var points = p.GetPoints();
                    return CollisionTools.TestCirclePolygon(posa, ra, points, out normal, out point);
                }
            }
            else if (this is OBBCollision rect)
            {
                if (collision is CircleCollision b)
                    return CollisionTools.TestCircleOBB(posb, rb, posa, ra, rect.size / 2, rect.up, out normal, out point);
                else if (collision is OBBCollision rect2)
                    return CollisionTools.TestOBBOBB(posa, ra, rect.size / 2, rect.up, posb, rb, rect2.size / 2, rect2.up,
                        out normal, out point);
            }
            else if (this is PolygonCollision p)
            {
                if (collision is CircleCollision b)
                {
                    var points = p.GetPoints();
                    return CollisionTools.TestCirclePolygon(posb, rb, points, out normal, out point);


                }
            }
            return false;
        }

        /// <summary>按具体形状分派射线窄相位；调用方负责提供有效方向。</summary>
        internal bool RayCast(LVector2 o, LVector2 d, out LVector2 hit, out LVector2 normal)
        {
            normal = LVector2.zero;
            hit = LVector2.zero;




            if (this is CircleCollision a)
                return CollisionTools.TestRayCircle(this.pos, a.radius, o, d, out hit, ref normal);

            else if (this is OBBCollision rect)
                return CollisionTools.TestRayOBB(o, d, this.bounds.center, rect.size / 2, rect.deg, out hit, ref normal);

            else if (this is PolygonCollision p)
            {
                var points = p.GetPoints();

                return CollisionTools.TestRayPolygon(o, d, points, out hit, ref normal);



            }


            return false;

        }

    }

}
