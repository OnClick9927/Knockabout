using System.Collections.Generic;
using Lockstep;
using Lockstep.Collision;
using UnityEngine;

namespace LockstepExamples.CollisionDemo3D
{
    /// <summary>
    /// LockstepEngine 3D 碰撞与射线查询示例。
    ///
    /// 运行时会动态创建球、旋转盒、胶囊和斜坡网格，并将它们加入
    /// CollisionTree3D。场景中的 Unity Collider 会被移除，因此所有颜色变化、
    /// 命中点和接触信息都来自锁步定点碰撞系统，而不是 Unity Physics。
    /// </summary>
    public class Collision3DExample : MonoBehaviour
    {
        [Header("重叠检测演示")]
        [SerializeField] private bool animateSphere = true;
        [SerializeField] private float animationSpeed = 1.25f;

        [Header("射线检测演示")]
        [Tooltip("射线的世界空间起点。")]
        [SerializeField] private Vector3 rayOrigin = new Vector3(-6f, 0.75f, 0f);
        [Tooltip("射线方向，不要求归一化。")]
        [SerializeField] private Vector3 rayDirection = Vector3.right;
        [Tooltip("有限射线的最大检测和绘制距离。")]
        [SerializeField, Min(0f)] private float rayDistance = 12f;

        // 查询列表在整个示例生命周期内复用，避免 Update 每帧产生 GC。
        private readonly List<Collision3DExampleShape> shapes =
            new List<Collision3DExampleShape>();
        private readonly List<CollisionResult3D> queryResults =
            new List<CollisionResult3D>();
        private readonly List<CollisionContact3D> contacts =
            new List<CollisionContact3D>();
        private readonly List<RayCastResult3D> rayResults =
            new List<RayCastResult3D>();

        private CollisionTree3D tree;
        private Collision3DExampleShape movingSphere;
        private Transform exampleRoot;
        private Mesh rampMesh;
        private Material exampleMaterial;

        private void Start()
        {
            // 场景文件只需要挂载本脚本；其余演示对象全部由代码创建，
            // 便于单独阅读本示例并验证每种 Collision3D 的构造参数。
            CreateCamera();
            CreateLight();
            CreateMaterial();
            CreateExampleObjects();

            tree = new CollisionTree3D();
            var layer = CollisionLayer.Get(0);
            for (var i = 0; i < shapes.Count; i++)
            {
                // 每个可见对象创建一个定点碰撞代理，并统一放在第 0 碰撞层。
                tree.Add(shapes[i].CreateAgent(layer));
                shapes[i].SetHit(false);
            }
        }

        private void Update()
        {
            if (tree == null) return;

            if (animateSphere && movingSphere != null)
            {
                // PingPong 只服务于演示。在真实锁步逻辑中，位置应由固定帧和定点速度驱动，
                // 不应直接使用 Time.time 作为权威模拟数据。
                var position = movingSphere.transform.position;
                position.x = -3.5f + Mathf.PingPong(Time.time * animationSpeed, 6f);
                movingSphere.transform.position = position;
            }

            for (var i = 0; i < shapes.Count; i++)
            {
                // 先同步所有代理，再一次性刷新包围盒，确保本帧查询读取同一时刻的数据。
                shapes[i].Sync();
                shapes[i].SetHit(false);
            }
            tree.Update();

            UpdateOverlapExample();
            UpdateRayCastExample();
        }

        /// <summary>
        /// 对每个形状执行一次重叠查询，并缓存用于 Gizmos 绘制的接触信息。
        /// </summary>
        private void UpdateOverlapExample()
        {
            contacts.Clear();
            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                tree.OverLap(shape.agent.collision, queryResults);
                for (var j = 0; j < queryResults.Count; j++)
                {
                    var result = queryResults[j];
                    var other = result.agent.userData as Collision3DExampleShape;
                    if (other == null) continue;

                    shape.SetHit(true);
                    other.SetHit(true);

                    // A 查 B 和 B 查 A 会得到同一对关系。
                    // 只保存 index 较小的一侧，避免 Gizmos 重复绘制接触点。
                    if (shape.index < other.index)
                        contacts.Add(result.contact);
                }
            }
        }

        /// <summary>
        /// 发射一条有限长度射线，并根据已按距离排序的结果标记命中对象。
        /// </summary>
        private void UpdateRayCastExample()
        {
            // 射线的起点、方向和最大距离都转换为定点类型后再进入查询。
            // CollisionTree3D.RayCast 会归一化方向并清空复用的 rayResults 列表。
            tree.RayCast(
                rayOrigin.ToLVector3(),
                rayDirection.ToLVector3(),
                LMath.ToLFloat(Mathf.Max(0f, rayDistance)),
                rayResults);

            for (var i = 0; i < rayResults.Count; i++)
            {
                var shape = rayResults[i].agent.userData as Collision3DExampleShape;
                if (shape != null)
                    shape.SetRayHit(i == 0);
            }
        }

        private void OnDrawGizmos()
        {
            // 即使尚未进入 Play Mode，也显示 Inspector 中配置的射线，方便调整参数。
            DrawRayGizmos();
            if (tree == null) return;

            // 绿色线框表示锁步碰撞体计算出的世界空间 AABB，而非 Unity Collider.bounds。
            Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.7f);
            for (var i = 0; i < shapes.Count; i++)
            {
                var agent = shapes[i].agent;
                if (agent == null) continue;
                var bounds = agent.bounds;
                Gizmos.DrawWireCube(bounds.center.ToVector3(), bounds.size.ToVector3());
            }

            for (var i = 0; i < contacts.Count; i++)
            {
                var contact = contacts[i];
                var pointA = contact.pointA.ToVector3();
                var pointB = contact.pointB.ToVector3();
                var middle = (pointA + pointB) * 0.5f;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pointA, pointB);
                Gizmos.DrawSphere(pointA, 0.06f);
                Gizmos.DrawSphere(pointB, 0.06f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(middle, contact.normal.ToVector3() * 0.75f);
            }
        }

        private void DrawRayGizmos()
        {
            var direction = rayDirection.normalized;
            var distance = Mathf.Max(0f, rayDistance);
            var end = rayOrigin + direction * distance;

            // 白线表示查询的完整范围；粉色线段表示起点到最近交点的距离。
            Gizmos.color = Color.white;
            Gizmos.DrawLine(rayOrigin, end);
            Gizmos.DrawWireSphere(rayOrigin, 0.08f);

            if (rayResults.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.72f, 1f);
                Gizmos.DrawLine(rayOrigin, rayResults[0].hitPoint.ToVector3());
            }

            for (var i = 0; i < rayResults.Count; i++)
            {
                var result = rayResults[i];
                var point = result.hitPoint.ToVector3();
                var normal = result.normal.ToVector3();

                Gizmos.color = i == 0
                    ? new Color(1f, 0.2f, 0.72f, 1f)
                    : new Color(1f, 0.72f, 0.12f, 1f);
                Gizmos.DrawSphere(point, 0.09f);

                // 法线从命中表面向外绘制；网格法线会自动翻到射线来向一侧。
                Gizmos.DrawRay(point, normal * 0.65f);
            }
        }

        private void OnDestroy()
        {
            if (tree != null)
            {
                // Clear 会回收代理及其 Collision3D；必须在清空组件引用之前调用。
                tree.Clear();
                tree = null;
            }
            for (var i = 0; i < shapes.Count; i++)
                shapes[i].ReleaseAgent();

            if (rampMesh != null) Destroy(rampMesh);
            if (exampleMaterial != null) Destroy(exampleMaterial);
        }

        private void CreateExampleObjects()
        {
            exampleRoot = new GameObject("Collision 3D Shapes").transform;
            exampleRoot.SetParent(transform, false);

            CreateGround();
            movingSphere = CreatePrimitive(
                PrimitiveType.Sphere,
                "Sphere (moving)",
                new Vector3(-3.5f, 0.75f, 0f),
                Vector3.one * 1.5f,
                Collision3DExampleShapeType.Sphere);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Box (rotated OBB)",
                new Vector3(-0.5f, 0.75f, 0f),
                Vector3.one * 1.5f,
                Collision3DExampleShapeType.Box,
                new Vector3(0f, 28f, 12f));
            CreatePrimitive(
                PrimitiveType.Capsule,
                "Capsule",
                new Vector3(1f, 1.5f, 2.8f),
                Vector3.one,
                Collision3DExampleShapeType.Capsule,
                new Vector3(0f, 0f, -18f));
            CreateRampMesh();
        }

        private Collision3DExampleShape CreatePrimitive(
            PrimitiveType primitiveType,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Collision3DExampleShapeType shapeType,
            Vector3 eulerAngles = default(Vector3))
        {
            // Unity Primitive 仅用来提供网格和 Renderer。
            var instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            instance.transform.SetParent(exampleRoot, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(eulerAngles);
            instance.transform.localScale = scale;

            var unityCollider = instance.GetComponent<Collider>();
            // 移除原生 Collider，确保示例不会误用 Unity Physics 的检测结果。
            if (unityCollider != null) Destroy(unityCollider);
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null && exampleMaterial != null)
                renderer.sharedMaterial = exampleMaterial;

            return AddShape(instance, shapeType);
        }

        private void CreateRampMesh()
        {
            var instance = new GameObject("Mesh Collider (ramp)");
            instance.transform.SetParent(exampleRoot, false);
            instance.transform.position = new Vector3(1f, 0f, 2.8f);

            rampMesh = new Mesh { name = "Collision3D Example Ramp" };
            // 四个顶点组成两个三角形。MeshCollision3D 会保留三角形索引，
            // 射线结果的 feature 字段可据此返回命中的三角形序号。
            rampMesh.vertices = new[]
            {
                new Vector3(-2f, 0f, -1f),
                new Vector3(2f, 0f, -1f),
                new Vector3(2f, 1f, 1f),
                new Vector3(-2f, 1f, 1f)
            };
            rampMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            rampMesh.RecalculateNormals();
            rampMesh.RecalculateBounds();

            var filter = instance.AddComponent<MeshFilter>();
            filter.sharedMesh = rampMesh;
            var renderer = instance.AddComponent<MeshRenderer>();
            if (exampleMaterial != null)
                renderer.sharedMaterial = exampleMaterial;
            AddShape(instance, Collision3DExampleShapeType.Mesh);
        }

        private Collision3DExampleShape AddShape(
            GameObject instance, Collision3DExampleShapeType shapeType)
        {
            // index 在创建期间固定不变，可作为示例内部的稳定关系排序键。
            var shape = instance.AddComponent<Collision3DExampleShape>();
            shape.Configure(shapeType, shapes.Count);
            shapes.Add(shape);
            return shape;
        }

        private void CreateGround()
        {
            // 地面只提供空间参照，不加入 shapes，也不会参与重叠或射线查询。
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Visual Ground (not a collider)";
            ground.transform.SetParent(exampleRoot, false);
            ground.transform.position = new Vector3(0f, -0.04f, 1f);
            ground.transform.localScale = new Vector3(0.9f, 1f, 0.7f);
            var unityCollider = ground.GetComponent<Collider>();
            if (unityCollider != null) Destroy(unityCollider);
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (exampleMaterial != null)
                    renderer.sharedMaterial = exampleMaterial;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                var color = new Color(0.16f, 0.18f, 0.2f, 1f);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }

        private void CreateMaterial()
        {
            // 优先使用项目当前的 URP Lit，同时保留内置管线和最低限度的回退 Shader。
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
                exampleMaterial = new Material(shader) { name = "Collision3D Example Material" };
        }

        private static void CreateCamera()
        {
            // 如果场景已经配置主相机则复用，避免出现两个 AudioListener。
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(7.5f, 6.5f, -10.5f);
            camera.transform.LookAt(new Vector3(0f, 1f, 1.2f));
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
        }

        private static void CreateLight()
        {
            // 示例场景为空时自动补一盏方向光，让三类 Primitive 和斜坡法线可见。
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
