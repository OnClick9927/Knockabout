using System;
using System.Collections.Generic;
using Lockstep;
using Lockstep.Collision;
using UnityEngine;

namespace LockstepExamples.CollisionDemo2D
{
    /// <summary>
    /// 2D 碰撞示例中的可视化形状。
    /// Unity 的 Transform 和 Renderer 只用于编辑与显示，真正的碰撞形状、位置、角度和查询
    /// 全部交给 Lockstep 定点碰撞系统处理。
    /// </summary>
    public class Agent : MonoBehaviour
    {
        public enum Shape
        {
            Circle,
            OBB,
            Polygon
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("锁步碰撞形状")]
        [SerializeField] private Shape shape;
        [Tooltip("圆形的基础半径。Transform 的 X/Z 缩放会继续作用于此值。")]
        [SerializeField] private LFloat r = LFloat.one;
        [Tooltip("OBB 的基础完整尺寸，X 对应世界 X，Y 对应世界 Z。")]
        [SerializeField] private LVector2 size = LVector2.one;
        [Tooltip("多边形的局部空间顶点，至少需要三个点。")]
        [SerializeField] private List<LVector2> points = new List<LVector2>();

        private MaterialPropertyBlock propertyBlock;
        private Renderer cachedRenderer;
        private Color gizmoColor = new Color(0.12f, 0.62f, 0.68f, 1f);

        /// <summary>当前对象在锁步四叉树中的代理；树销毁后该引用会被清空。</summary>
        public CollisionAgent agent { get; private set; }

        /// <summary>场景列表中的稳定序号，用于避免同一对盒体被记录两次。</summary>
        public int index { get; private set; }

        public Shape shapeType => shape;

        /// <summary>Unity 绕 Y 轴的角度映射到 2D XZ 平面的旋转角度。</summary>
        public LFloat deg => LMath.ToLFloat(transform.rotation.eulerAngles.y);

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            cachedRenderer = GetComponent<Renderer>();
        }

        /// <summary>记录由示例控制器分配的稳定序号。</summary>
        public void Configure(int shapeIndex)
        {
            index = shapeIndex;
            if (cachedRenderer == null)
                cachedRenderer = GetComponent<Renderer>();
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 从 Inspector 参数创建锁步碰撞体，并把当前组件写入 userData。
        /// 查询结果可由 userData 直接定位到可视化对象，不需要额外字典。
        /// </summary>
        public CollisionAgent Create(CollisionLayer layer)
        {
            Lockstep.Collision.Collision collision;
            LVector2 position = transform.position.ToLVector2XZ();
            Vector3 scale = Abs(transform.lossyScale);

            switch (shape)
            {
                case Shape.OBB:
                    collision = OBBCollision.New(position, ScaledBoxSize(scale), deg);
                    break;
                case Shape.Circle:
                    collision = CircleCollision.New(
                        position,
                        r * LMath.ToLFloat(Mathf.Max(scale.x, scale.z)));
                    break;
                case Shape.Polygon:
                    if (points == null || points.Count < 3)
                        throw new InvalidOperationException(
                            $"Polygon example '{name}' requires at least three local points.");
                    collision = PolygonCollision.New(position, points, deg);
                    collision.SetScale(LMath.ToLFloat(Mathf.Max(scale.x, scale.z)));
                    collision.CalcBounds();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            agent = collision.MakeAgent(layer, this);
            return agent;
        }

        /// <summary>
        /// 把最新的 Unity Transform 写入锁步代理。所有对象同步完成后，控制器只调用一次
        /// CollisionTree.Update，使本帧的包围盒迁移和查询读取同一时刻的数据。
        /// </summary>
        public void Sync()
        {
            if (agent == null) return;

            Vector3 scale = Abs(transform.lossyScale);
            agent.SetPos(transform.position.ToLVector2XZ());
            agent.SetDeg(deg);

            switch (shape)
            {
                case Shape.OBB:
                    agent.SetSize(ScaledBoxSize(scale));
                    break;
                case Shape.Circle:
                    agent.SetRadius(r * LMath.ToLFloat(Mathf.Max(scale.x, scale.z)));
                    break;
                case Shape.Polygon:
                    agent.SetScale(LMath.ToLFloat(Mathf.Max(scale.x, scale.z)));
                    break;
            }
        }

        /// <summary>恢复未命中状态；青色表示当前没有参与盒体重叠或有限射线命中。</summary>
        public void ResetState()
        {
            SetColor(new Color(0.12f, 0.62f, 0.68f, 1f));
        }

        /// <summary>红色表示当前 OBB 正与另一个 OBB 重叠。</summary>
        public void SetBoxOverlap()
        {
            SetColor(new Color(0.92f, 0.22f, 0.16f, 1f));
        }

        /// <summary>粉色表示最近射线命中，黄色表示同一有限射线上的其余命中。</summary>
        public void SetRayHit(bool nearest)
        {
            SetColor(nearest
                ? new Color(1f, 0.2f, 0.72f, 1f)
                : new Color(1f, 0.72f, 0.12f, 1f));
        }

        private void SetColor(Color color)
        {
            gizmoColor = color;
            if (cachedRenderer == null) return;

            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>树的 Clear 已负责回收代理和形状，此处只清除失效引用。</summary>
        public void ReleaseAgent()
        {
            agent = null;
        }

        private LVector2 ScaledBoxSize(Vector3 scale)
        {
            return new LVector2(
                size.x * LMath.ToLFloat(scale.x),
                size.y * LMath.ToLFloat(scale.z));
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Application.isPlaying ? gizmoColor : Color.yellow;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                Quaternion.Euler(0f, deg.ToFloat(), 0f),
                Abs(transform.lossyScale));

            switch (shape)
            {
                case Shape.OBB:
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        new Vector3(size.x.ToFloat(), 0f, size.y.ToFloat()));
                    break;
                case Shape.Circle:
                    Gizmos.DrawWireSphere(Vector3.zero, r.ToFloat());
                    break;
                case Shape.Polygon:
                    DrawPolygonGizmos();
                    break;
            }

            // Gizmos.matrix 是全局状态，绘制完成后必须恢复，避免影响其他组件。
            Gizmos.matrix = previousMatrix;
        }

        private void DrawPolygonGizmos()
        {
            if (points == null || points.Count < 2) return;

            for (int i = 0; i < points.Count; i++)
            {
                LVector2 a = points[i];
                LVector2 b = points[(i + 1) % points.Count];
                Gizmos.DrawLine(a.ToVector3XZ(), b.ToVector3XZ());
            }
        }
    }
}
