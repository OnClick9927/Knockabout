using System.Collections.Generic;

namespace Lockstep.Collision
{
    /// <summary>
    /// 二维多边形碰撞体。points 保存局部空间顶点，points_real 缓存经过缩放、
    /// 旋转和平移后的世界顶点。修改变换后必须 CalcBounds 才能刷新缓存和 AABB。
    /// 当前窄相位主要支持圆与多边形以及射线与多边形查询。
    /// </summary>
    public class PolygonCollision : Collision<PolygonCollision>
    {
        public List<LVector2> points;
        public LVector2[] points_real;
        public static PolygonCollision New(LVector2 pos, List<LVector2> points, LFloat deg)
        {
            PolygonCollision circle = New();
            circle.Init(pos, points, deg);

            return circle;
        }

        private void Init(LVector2 pos, List<LVector2> points,  LFloat deg) 
        {
            ready = false;
            base.Init(pos, LFloat.zero, deg);
            this.points = points;
            var count = points == null ? 0 : points.Count;
            if (points_real == null || points_real.Length != count)
                points_real = new LVector2[count];
            ready = true;
            CalcBounds();
        }
        private bool ready = false;

        public override bool SetRadius(LFloat size) => false;

        public override bool SetSize(LVector2 size) => false;

        public override void Cycle()
        {
            points = null;
            ready = false;
            base.Cycle();
        }

        public LVector2[] GetPoints() => points_real;
        /// <summary>
        /// 重建世界顶点、外接圆半径和轴对齐包围盒；空顶点集合退化为位置上的零尺寸形状。
        /// </summary>
        public override void CalcBounds()
        {
            if (!ready) return;
            if (points == null)
            {
                Set_Radius(LFloat.zero);
                base.CalcBounds();
                return;
            }
            var deg = this.deg;
            var scale = this.scale;
            var pos = this.pos;
            var count = points.Count;
            if (points_real == null || points_real.Length != count)
                points_real = new LVector2[count];

            if (count == 0)
            {
                Set_Radius(LFloat.zero);
                base.CalcBounds();
                return;
            }

            var maxSqrRadius = LFloat.zero;
            LFloat xMin = LFloat.MaxValue;
            LFloat yMin = LFloat.MaxValue;
            LFloat xMax = LFloat.MinValue;
            LFloat yMax = LFloat.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                maxSqrRadius = LMath.Max(maxSqrRadius, point.sqrMagnitude);
                point *= scale;
                point = point.Rotate(-deg);
                point += pos;
                points_real[i] = point;
                xMin = LMath.Min(xMin, point.x);
                yMin = LMath.Min(yMin, point.y);
                xMax = LMath.Max(xMax, point.x);
                yMax = LMath.Max(yMax, point.y);
            }
            Set_Radius(LMath.Sqrt(maxSqrRadius));
            bounds.Set(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
