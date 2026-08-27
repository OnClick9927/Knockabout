#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lockstep.Collision
{
    /// <summary>
    /// Unity 三维碰撞数据到 Lockstep 定点碰撞体的边界转换工具。
    /// <para>
    /// 整个文件受 <c>UNITY_5_3_OR_NEWER</c> 宏保护；在服务器或其他非 Unity 环境编译时，
    /// 预处理器会移除本文件全部 Unity 类型和实现，不会给 Lockstep 核心引入 UnityEngine 依赖。
    /// </para>
    /// <para>
    /// 所有方法都会创建一个新的池化 Lockstep 碰撞体。调用方拥有返回对象的生命周期：
    /// 若未交给 <see cref="CollisionAgent3D"/> 和碰撞树管理，使用结束后应主动调用
    /// <see cref="Collision3D.Cycle"/> 归还对象池。
    /// </para>
    /// </summary>
    public static class UnityCollision3DExtensions
    {
        /// <summary>
        /// 根据 Unity Collider 的实际派生类型创建对应的 Lockstep 三维碰撞体。
        /// 支持 BoxCollider、SphereCollider、CapsuleCollider 和 MeshCollider；其他 Collider
        /// 会抛出 <see cref="NotSupportedException"/>，避免静默使用不准确的包围盒替代真实形状。
        /// </summary>
        public static Collision3D ToLockstepCollision3D(this Collider source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is BoxCollider box)
                return box.ToLockstepCollision3D();
            if (source is SphereCollider sphere)
                return sphere.ToLockstepCollision3D();
            if (source is CapsuleCollider capsule)
                return capsule.ToLockstepCollision3D();
            if (source is MeshCollider mesh)
                return mesh.ToLockstepCollision3D();

            throw new NotSupportedException(
                $"Unity collider {source.GetType().FullName} has no Lockstep Collision3D converter.");
        }

        /// <summary>
        /// 尝试转换 Unity Collider。类型不受支持或 MeshCollider 没有 sharedMesh 时返回 false；
        /// 网格不可读等资源配置错误仍会抛出异常，便于调用方定位真正的数据问题。
        /// </summary>
        public static bool TryToLockstepCollision3D(
            this Collider source,
            out Collision3D collision)
        {
            collision = null;
            if (source == null) return false;

            if (source is BoxCollider box)
                collision = box.ToLockstepCollision3D();
            else if (source is SphereCollider sphere)
                collision = sphere.ToLockstepCollision3D();
            else if (source is CapsuleCollider capsule)
                collision = capsule.ToLockstepCollision3D();
            else if (source is MeshCollider mesh && mesh.sharedMesh != null)
                collision = mesh.ToLockstepCollision3D();

            return collision != null;
        }

        /// <summary>
        /// 把 Unity BoxCollider 转为 Lockstep 有向盒。
        /// center 使用完整 Transform 变换到世界空间；size 分量乘 lossyScale 的绝对值，
        /// rotation 直接使用世界四元数，因此普通父子缩放与旋转可以保持一致。
        /// </summary>
        public static BoxCollision3D ToLockstepCollision3D(this BoxCollider source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Transform transform = source.transform;
            Vector3 scale = Abs(transform.lossyScale);
            Vector3 worldSize = Vector3.Scale(source.size, scale);
            Vector3 worldCenter = transform.TransformPoint(source.center);
            return BoxCollision3D.New(
                worldCenter.ToLVector3(),
                worldSize.ToLVector3(),
                transform.rotation.ToLQuaternion());
        }

        /// <summary>
        /// 把 Unity SphereCollider 转为 Lockstep 球体。
        /// Unity 在非均匀缩放下以最大绝对缩放分量扩张球半径，本方法沿用相同规则。
        /// </summary>
        public static SphereCollision3D ToLockstepCollision3D(this SphereCollider source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Transform transform = source.transform;
            Vector3 scale = Abs(transform.lossyScale);
            float radiusScale = MaxComponent(scale);
            return SphereCollision3D.New(
                transform.TransformPoint(source.center).ToLVector3(),
                LMath.ToLFloat(source.radius * radiusScale));
        }

        /// <summary>
        /// 把 Unity CapsuleCollider 转为 Lockstep 胶囊。
        /// direction 决定 Unity 胶囊的局部主轴；Lockstep 胶囊固定以局部 Y 为主轴，因此这里会把
        /// X/Z 主轴旋转到对应世界方向。半径采用两个垂直轴中的较大缩放，高度采用主轴缩放。
        /// </summary>
        public static CapsuleCollision3D ToLockstepCollision3D(this CapsuleCollider source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Transform transform = source.transform;
            Vector3 scale = Abs(transform.lossyScale);
            GetCapsuleScale(
                source.direction,
                scale,
                out float radiusScale,
                out float heightScale,
                out Quaternion axisRotation);

            return CapsuleCollision3D.New(
                transform.TransformPoint(source.center).ToLVector3(),
                LMath.ToLFloat(source.radius * radiusScale),
                LMath.ToLFloat(source.height * heightScale),
                (transform.rotation * axisRotation).ToLQuaternion());
        }

        /// <summary>
        /// 把 Unity MeshCollider 的 sharedMesh 按当前 localToWorldMatrix 精确烘焙为世界空间定点网格。
        /// 这种方式支持非均匀缩放和负缩放，不依赖 MeshCollision3D 的统一 scale 限制。
        /// </summary>
        public static MeshCollision3D ToLockstepCollision3D(this MeshCollider source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.sharedMesh == null)
                throw new ArgumentException("MeshCollider must reference a sharedMesh.", nameof(source));

            return source.sharedMesh.ToLockstepCollision3D(
                source.transform.localToWorldMatrix);
        }

        /// <summary>
        /// 把 Unity Mesh 的本地顶点直接转换为位于世界原点、无旋转、单位缩放的 MeshCollision3D。
        /// Mesh 必须启用 Read/Write；编辑器若需要读取不可读导入资源，应使用 MeshUtility 获取只读快照。
        /// </summary>
        public static MeshCollision3D ToLockstepCollision3D(this Mesh source)
        {
            return source.ToLockstepCollision3D(Matrix4x4.identity);
        }

        /// <summary>使用指定 Transform 把 Unity Mesh 烘焙为世界空间 MeshCollision3D。</summary>
        public static MeshCollision3D ToLockstepCollision3D(
            this Mesh source,
            Transform transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            return source.ToLockstepCollision3D(transform.localToWorldMatrix);
        }

        /// <summary>
        /// 使用指定本地到世界矩阵转换 Unity Mesh。
        /// 仅收集三角形拓扑的子网格；点、线等拓扑不会进入三维网格碰撞体。
        /// </summary>
        public static MeshCollision3D ToLockstepCollision3D(
            this Mesh source,
            Matrix4x4 localToWorld)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.isReadable)
            {
                throw new InvalidOperationException(
                    $"Mesh '{source.name}' is not readable. Enable Read/Write or use an editor read-only mesh snapshot.");
            }

            Vector3[] sourceVertices = source.vertices;
            var vertices = new LVector3[sourceVertices.Length];
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                vertices[i] = localToWorld.MultiplyPoint3x4(sourceVertices[i]).ToLVector3();
            }

            var triangles = new List<int>();
            for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                if (source.GetTopology(subMesh) != MeshTopology.Triangles) continue;

                // 单参数重载兼容更早的 Unity 版本；返回值已经按该子网格的 baseVertex 修正。
                int[] indices = source.GetIndices(subMesh);
                triangles.AddRange(indices);
            }

            return MeshCollision3D.New(
                LVector3.zero,
                vertices,
                triangles.ToArray());
        }

        private static void GetCapsuleScale(
            int direction,
            Vector3 scale,
            out float radiusScale,
            out float heightScale,
            out Quaternion axisRotation)
        {
            switch (direction)
            {
                case 0:
                    radiusScale = Mathf.Max(scale.y, scale.z);
                    heightScale = scale.x;
                    axisRotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                    return;
                case 1:
                    radiusScale = Mathf.Max(scale.x, scale.z);
                    heightScale = scale.y;
                    axisRotation = Quaternion.identity;
                    return;
                case 2:
                    radiusScale = Mathf.Max(scale.x, scale.y);
                    heightScale = scale.z;
                    axisRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "CapsuleCollider direction must be 0 (X), 1 (Y), or 2 (Z).");
            }
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }

        private static float MaxComponent(Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }
    }
}
#endif
