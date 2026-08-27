using Lockstep;
using Lockstep.Collision;
using UnityEngine;

namespace LockstepExamples.CollisionDemo3D
{
    /// <summary>
    /// 示例对象使用的锁步碰撞形状类型。
    /// Unity 原生 Collider 只负责创建可见模型，实际查询完全由这里选择的类型决定。
    /// </summary>
    public enum Collision3DExampleShapeType
    {
        Sphere,
        Box,
        Capsule,
        Mesh
    }

    public class Collision3DExampleShape : MonoBehaviour
    {
        // 同时设置 URP 与内置渲染管线常用的颜色属性，
        // 这样示例切换渲染管线后仍能正确显示碰撞状态。
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock propertyBlock;
        private Renderer cachedRenderer;
        private Collision3DExampleShapeType shapeType;

        private void Awake()
        {
            // MaterialPropertyBlock 只覆盖当前 Renderer 的颜色，
            // 不会为每个物体复制一份 Material，适合逐帧更新调试颜色。
            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>该 Unity 对象在锁步碰撞树中的代理。</summary>
        public CollisionAgent3D agent { get; private set; }

        /// <summary>
        /// 对象创建时分配的稳定序号，用于避免一对重叠关系被重复记录。
        /// </summary>
        public int index { get; private set; }

        /// <summary>
        /// 在创建锁步代理前记录形状类型，并缓存只用于显示的 Renderer。
        /// </summary>
        public void Configure(Collision3DExampleShapeType type, int shapeIndex)
        {
            shapeType = type;
            index = shapeIndex;
            cachedRenderer = GetComponent<Renderer>();
        }

        public CollisionAgent3D CreateAgent(CollisionLayer layer)
        {
            Collision3D collision;

            // 所有参与判定的数据都从 Unity 浮点类型转换为 Lockstep 定点类型。
            // 真正的战斗逻辑应直接维护定点位置；示例为了方便拖动物体才从 Transform 同步。
            var position = transform.position.ToLVector3();
            var rotation = ToLQuaternion(transform.rotation.eulerAngles);
            var scale = Abs(transform.lossyScale);

            switch (shapeType)
            {
                case Collision3DExampleShapeType.Sphere:
                    // Unity Sphere 的原始直径为 1，因此半径取最大缩放轴的一半。
                    collision = SphereCollision3D.New(
                        position, LMath.ToLFloat(MaxComponent(scale) * 0.5f));
                    break;
                case Collision3DExampleShapeType.Box:
                    // Unity Cube 的原始边长为 1，lossyScale 可直接作为盒体完整尺寸。
                    collision = BoxCollision3D.New(position, scale.ToLVector3(), rotation);
                    break;
                case Collision3DExampleShapeType.Capsule:
                    // Unity Capsule 沿局部 Y 轴，高度为 2、直径为 1。
                    // 引擎胶囊的 height 表示包含两个半球在内的总高度。
                    collision = CapsuleCollision3D.New(
                        position,
                        LMath.ToLFloat(Mathf.Max(scale.x, scale.z) * 0.5f),
                        LMath.ToLFloat(scale.y * 2f),
                        rotation);
                    break;
                case Collision3DExampleShapeType.Mesh:
                    var filter = GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                        throw new MissingComponentException("Mesh example requires a MeshFilter with a mesh.");

                    // MeshCollision3D 接收局部空间顶点和三角形索引。
                    // 顶点只在创建时转换一次，后续位移、旋转、统一缩放由碰撞体处理。
                    var mesh = filter.sharedMesh;
                    var vertices = new LVector3[mesh.vertexCount];
                    var sourceVertices = mesh.vertices;
                    for (var i = 0; i < sourceVertices.Length; i++)
                        vertices[i] = sourceVertices[i].ToLVector3();
                    collision = MeshCollision3D.New(position, vertices, mesh.triangles, rotation);

                    // 当前 MeshCollision3D 只支持统一缩放，因此示例使用 X 轴缩放值。
                    collision.SetScale(LMath.ToLFloat(scale.x));
                    collision.CalcBounds();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            // userData 保存当前组件，查询命中后无需额外字典即可找回场景对象。
            agent = collision.MakeAgent(layer, this);
            return agent;
        }

        /// <summary>
        /// 把场景 Transform 的最新状态写入锁步代理。
        /// 调用后还需要执行 CollisionTree3D.Update，树中的包围盒才会刷新。
        /// </summary>
        public void Sync()
        {
            if (agent == null) return;

            var scale = Abs(transform.lossyScale);
            agent.SetPos(transform.position.ToLVector3());
            agent.SetRotation(ToLQuaternion(transform.rotation.eulerAngles));
            switch (shapeType)
            {
                case Collision3DExampleShapeType.Sphere:
                    agent.SetRadius(LMath.ToLFloat(MaxComponent(scale) * 0.5f));
                    break;
                case Collision3DExampleShapeType.Box:
                    agent.SetSize(scale.ToLVector3());
                    break;
                case Collision3DExampleShapeType.Capsule:
                    agent.SetRadius(LMath.ToLFloat(Mathf.Max(scale.x, scale.z) * 0.5f));
                    agent.SetHeight(LMath.ToLFloat(scale.y * 2f));
                    break;
                case Collision3DExampleShapeType.Mesh:
                    agent.SetScale(LMath.ToLFloat(scale.x));
                    break;
            }
        }

        public void SetHit(bool hit)
        {
            var color = hit
                ? new Color(0.92f, 0.22f, 0.16f, 1f)
                : new Color(0.12f, 0.62f, 0.68f, 1f);
            SetColor(color);
        }

        /// <summary>
        /// 用独立颜色标记射线结果：粉色为最近命中，黄色为其余命中。
        /// 此方法在重叠检测之后调用，所以射线状态会优先显示。
        /// </summary>
        public void SetRayHit(bool nearest)
        {
            SetColor(nearest
                ? new Color(1f, 0.2f, 0.72f, 1f)
                : new Color(1f, 0.72f, 0.12f, 1f));
        }

        private void SetColor(Color color)
        {
            if (cachedRenderer == null) return;

            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }

        public void ReleaseAgent()
        {
            // 代理本体由 CollisionTree3D.Clear 回收到对象池；这里只清除失效引用。
            agent = null;
        }

        private static LQuaternion ToLQuaternion(Vector3 eulerAngles)
        {
            // 示例使用欧拉角是为了方便 Inspector 编辑；引擎内部仍保存定点四元数。
            return LQuaternion.Euler(
                LMath.ToLFloat(eulerAngles.x),
                LMath.ToLFloat(eulerAngles.y),
                LMath.ToLFloat(eulerAngles.z));
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static float MaxComponent(Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }
    }
}
