
#if UNITY_5_3_OR_NEWER
using UnityEngine;


namespace Lockstep.Collision
{
    /// <summary>仅在 Unity 环境中使用的碰撞树 Gizmos 绘制辅助工具。</summary>
    class GizmosTools
    {
        /// <summary>根据碰撞平面把二维矩形绘制到 Unity XY 或 XZ 世界平面。</summary>
        public static void DrawRect(LRect rect, UnityEngine.Color color, CollisionType type)
        {

            Gizmos.color = color;
            var _a = rect.position;
            var _c = rect.max;
            var _b = new LVector2(rect.xMax, rect.y);
            var _d = new LVector2(rect.x, rect.yMax);


            var a = type == CollisionType.XY ? _a.ToVector3() : _a.ToVector3XZ();
            var b = type == CollisionType.XY ? _b.ToVector3():  _b.ToVector3XZ();
            var c = type == CollisionType.XY ? _c.ToVector3() : _c.ToVector3XZ();
            var d = type == CollisionType.XY ? _d.ToVector3(): _d.ToVector3XZ();
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            //Gizmos.color = Color.white;
        }


    }

}
#endif
