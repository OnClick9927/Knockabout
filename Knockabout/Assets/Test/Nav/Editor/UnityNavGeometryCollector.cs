using System;
using System.Collections.Generic;
using Lockstep;
using Lockstep.Collision;
using Lockstep.Nav;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RGBC.Navigation.UnityEditor
{
    /// <summary>
    /// Unity GameObject 到纯 Collision3D 导航输入之间的编辑器适配器。
    /// <para>本类不扫描场景层级、根节点、Layer 或标记组件。调用方必须显式传入已经筛选并分组的
    /// 不可行走对象、可行走对象和链接；本类只读取每个对象自身的 Collider、MeshFilter 或 Terrain，
    /// 再把它们转换为 <see cref="Collision3D"/>。</para>
    /// <para>把 Unity 类型严格限制在此文件后，LockstepEngine 导航模块可在服务器、命令行工具
    /// 或其他引擎中直接使用，只需由宿主提供筛选完成的三维碰撞体与链接数据。</para>
    /// </summary>
    internal static class UnityNavGeometryCollector
    {
        /// <summary>
        /// 把显式传入的 Unity 对象转换为临时 Lockstep 碰撞体，然后调用纯数据导航构建入口。
        /// <para>
        /// 本方法不会递归读取子节点；列表中的每个 GameObject 都是一项明确输入。
        /// 同一对象同时出现在两组时以不可行走组为准，同组重复项只处理一次。
        /// </para>
        /// <para>
        /// 参数顺序固定为：构建设置、不可行走对象、可行走对象、跳跃链接。不可行走对象仍作为
        /// 实体参加占用和净空计算，但包括顶部在内的所有表面都不会生成可行走导航面。
        /// </para>
        /// </summary>
        public static NavData Build(
            NavBuildSettings settings,
            IList<GameObject> nonWalkableObjects,
            IList<GameObject> walkableObjects,
            IList<NavBuildLink> links,
            out NavBuildReport report)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // 采集状态只在本次 Build 内存活，不使用线程局部或静态缓存。
            int obstacleCapacity = GetCount(nonWalkableObjects);
            int walkCapacity = GetCount(walkableObjects);
            var obstacles = new List<Collision3D>(obstacleCapacity);
            var walks = new List<Collision3D>(walkCapacity);
            var ownedCollisions = new List<Collision3D>(obstacleCapacity + walkCapacity);
            var obstacleObjects = new HashSet<GameObject>();
            var convertedObjects = new HashSet<GameObject>();
            var colliderBuffer = new List<Collider>(4);
            try
            {
                CollectCollisions(
                    nonWalkableObjects,
                    walkableObjects,
                    obstacles,
                    walks,
                    ownedCollisions,
                    obstacleObjects,
                    convertedObjects,
                    colliderBuffer);

                return NavBuilder.Build(
                    settings,
                    links,
                    obstacles,
                    walks,
                    out report);
            }
            finally
            {
                // 这些碰撞体只是本次离线构建的临时输入，NavBuilder 不拥有其生命周期。
                for (int i = 0; i < ownedCollisions.Count; i++)
                    ownedCollisions[i].Cycle();
            }
        }

        /// <summary>
        /// 收集两个显式对象列表。先建立不可行走集合，再处理可行走组，保证重复对象始终采用
        /// 更严格的不可行走语义；最后处理不可行走组本身。
        /// </summary>
        private static void CollectCollisions(
            IList<GameObject> nonWalkableObjects,
            IList<GameObject> walkableObjects,
            List<Collision3D> obstacles,
            List<Collision3D> walks,
            List<Collision3D> ownedCollisions,
            HashSet<GameObject> obstacleObjects,
            HashSet<GameObject> convertedObjects,
            List<Collider> colliderBuffer)
        {
            FillObjectSet(
                nonWalkableObjects,
                nameof(nonWalkableObjects),
                obstacleObjects);

            AppendObjects(
                walkableObjects,
                nameof(walkableObjects),
                false,
                obstacleObjects,
                convertedObjects,
                walks,
                ownedCollisions,
                colliderBuffer);
            AppendObjects(
                nonWalkableObjects,
                nameof(nonWalkableObjects),
                true,
                null,
                convertedObjects,
                obstacles,
                ownedCollisions,
                colliderBuffer);
        }

        /// <summary>
        /// 校验对象列表并建立引用集合。列表为 null 等同于空列表；列表项为 null 则视为明确配置错误。
        /// </summary>
        private static void FillObjectSet(
            IList<GameObject> source,
            string parameterName,
            HashSet<GameObject> result)
        {
            if (source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                GameObject gameObject = source[i];
                if (gameObject == null)
                    throw new ArgumentException(
                        $"GameObject at index {i} is null.",
                        parameterName);
                result.Add(gameObject);
            }
        }

        /// <summary>
        /// 按调用方列表顺序处理对象。skip 用来跳过属于更高优先级分组的对象，convertedObjects
        /// 阻止同一 GameObject 因重复填写而多次提交相同几何。
        /// </summary>
        private static void AppendObjects(
            IList<GameObject> source,
            string parameterName,
            bool isObstacle,
            HashSet<GameObject> skip,
            HashSet<GameObject> convertedObjects,
            List<Collision3D> destination,
            List<Collision3D> ownedCollisions,
            List<Collider> colliderBuffer)
        {
            if (source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                GameObject gameObject = source[i];
                if (gameObject == null)
                    throw new ArgumentException(
                        $"GameObject at index {i} is null.",
                        parameterName);
                if (skip != null && skip.Contains(gameObject)) continue;
                if (!convertedObjects.Add(gameObject)) continue;

                int firstOwnedIndex = ownedCollisions.Count;
                AppendObjectCollisions(
                    gameObject,
                    destination,
                    ownedCollisions,
                    colliderBuffer);
                if (ownedCollisions.Count == firstOwnedIndex)
                {
                    string groupName = isObstacle ? "non-walkable" : "walkable";
                    throw new InvalidOperationException(
                        $"Explicit {groupName} object '{gameObject.name}' has no enabled supported " +
                        "Collider, MeshFilter, or Terrain on the object itself.");
                }
            }
        }

        /// <summary>
        /// 读取单个 GameObject 自身的几何组件。
        /// <para>
        /// Collider 优先于 MeshFilter，防止同一模型的碰撞网格与渲染网格重复输入；Terrain 单独处理。
        /// 多个受支持 Collider 会全部保留，顺序与 GameObject 上的组件顺序一致。
        /// </para>
        /// </summary>
        private static void AppendObjectCollisions(
            GameObject gameObject,
            List<Collision3D> destination,
            List<Collision3D> ownedCollisions,
            List<Collider> colliderBuffer)
        {
            bool addedCollider = false;
            colliderBuffer.Clear();
            gameObject.GetComponents(colliderBuffer);
            for (int i = 0; i < colliderBuffer.Count; i++)
            {
                Collider collider = colliderBuffer[i];
                if (!collider.enabled || collider.isTrigger) continue;

                // TerrainCollider 的几何由 TerrainData 精确生成，不能按通用 Collider 转换。
                if (collider is TerrainCollider) continue;

                Collision3D collision;
                if (collider is MeshCollider meshCollider)
                {
                    if (meshCollider.sharedMesh == null) continue;
                    collision = CreateMeshCollision(
                        meshCollider.sharedMesh,
                        meshCollider.transform.localToWorldMatrix);
                }
                else
                {
                    collision = collider.ToLockstepCollision3D();
                }

                destination.Add(collision);
                ownedCollisions.Add(collision);
                addedCollider = true;
            }

            Terrain terrain = gameObject.GetComponent<Terrain>();
            if (terrain != null && terrain.enabled && terrain.terrainData != null)
            {
                Collision3D collision = CreateTerrainCollision(terrain);
                destination.Add(collision);
                ownedCollisions.Add(collision);
                return;
            }

            if (addedCollider) return;

            MeshFilter filter = gameObject.GetComponent<MeshFilter>();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (filter == null || filter.sharedMesh == null ||
                (renderer != null && !renderer.enabled))
                return;

            Collision3D meshCollision = CreateMeshCollision(
                filter.sharedMesh,
                filter.transform.localToWorldMatrix);
            destination.Add(meshCollision);
            ownedCollisions.Add(meshCollision);
        }

        private static MeshCollision3D CreateMeshCollision(
            Mesh mesh,
            Matrix4x4 localToWorld)
        {
            // 编辑器只读 MeshData 同时支持可读与不可读资源。相比 mesh.vertices/GetIndices，
            // 它不会创建每个子网格的托管数组，并允许直接写入最终长度的世界顶点和索引数组。
            using (Mesh.MeshDataArray meshDataArray = MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                Mesh.MeshData meshData = meshDataArray[0];
                using (var sourceVertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp))
                {
                    meshData.GetVertices(sourceVertices);
                    var vertices = new LVector3[sourceVertices.Length];
                    for (int i = 0; i < sourceVertices.Length; i++)
                    {
                        vertices[i] = ToLVector3(
                            localToWorld.MultiplyPoint3x4(sourceVertices[i]));
                    }

                    int[] indices = new int[CountTriangleIndices(meshData)];
                    if (meshData.indexFormat == IndexFormat.UInt16)
                    {
                        NativeArray<ushort> sourceIndices = meshData.GetIndexData<ushort>();
                        AppendSubMeshIndices(
                            meshData,
                            sourceIndices,
                            indices);
                    }
                    else
                    {
                        NativeArray<uint> sourceIndices = meshData.GetIndexData<uint>();
                        AppendSubMeshIndices(
                            meshData,
                            sourceIndices,
                            indices);
                    }

                    // vertices 已烘焙为世界坐标，直接把数组所有权交给临时碰撞体，避免构造时
                    // 再 Clone 两个数组并创建一份相同长度的 worldVertices。
                    return MeshCollision3D.NewWorldSpaceOwned(vertices, indices);
                }
            }
        }

        /// <summary>统计三角形拓扑所需的精确索引长度，供一次性分配最终数组。</summary>
        private static int CountTriangleIndices(Mesh.MeshData meshData)
        {
            long count = 0L;
            for (int subMesh = 0; subMesh < meshData.subMeshCount; subMesh++)
            {
                SubMeshDescriptor descriptor = meshData.GetSubMesh(subMesh);
                if (descriptor.topology != MeshTopology.Triangles) continue;
                count += descriptor.indexCount - descriptor.indexCount % 3;
            }

            if (count > int.MaxValue)
                throw new InvalidOperationException("Mesh triangle index count exceeds Int32 capacity.");
            return (int)count;
        }

        private static void AppendSubMeshIndices(
            Mesh.MeshData meshData,
            NativeArray<ushort> sourceIndices,
            int[] result)
        {
            int writeIndex = 0;
            for (int subMesh = 0; subMesh < meshData.subMeshCount; subMesh++)
            {
                SubMeshDescriptor descriptor = meshData.GetSubMesh(subMesh);
                if (descriptor.topology != MeshTopology.Triangles) continue;

                int end = descriptor.indexStart + descriptor.indexCount;
                for (int i = descriptor.indexStart; i + 2 < end; i += 3)
                {
                    result[writeIndex++] = descriptor.baseVertex + sourceIndices[i];
                    result[writeIndex++] = descriptor.baseVertex + sourceIndices[i + 1];
                    result[writeIndex++] = descriptor.baseVertex + sourceIndices[i + 2];
                }
            }
        }

        private static void AppendSubMeshIndices(
            Mesh.MeshData meshData,
            NativeArray<uint> sourceIndices,
            int[] result)
        {
            int writeIndex = 0;
            for (int subMesh = 0; subMesh < meshData.subMeshCount; subMesh++)
            {
                SubMeshDescriptor descriptor = meshData.GetSubMesh(subMesh);
                if (descriptor.topology != MeshTopology.Triangles) continue;

                int end = descriptor.indexStart + descriptor.indexCount;
                for (int i = descriptor.indexStart; i + 2 < end; i += 3)
                {
                    result[writeIndex++] = descriptor.baseVertex + (int)sourceIndices[i];
                    result[writeIndex++] = descriptor.baseVertex + (int)sourceIndices[i + 1];
                    result[writeIndex++] = descriptor.baseVertex + (int)sourceIndices[i + 2];
                }
            }
        }

        private static MeshCollision3D CreateTerrainCollision(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            Vector3 size = data.size;
            Matrix4x4 localToWorld = terrain.transform.localToWorldMatrix;
            var vertices = new LVector3[resolution * resolution];
            var indices = new int[(resolution - 1) * (resolution - 1) * 6];

            for (int z = 0; z < resolution; z++)
            {
                float localZ = size.z * z / (resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    float localX = size.x * x / (resolution - 1);
                    Vector3 world = localToWorld.MultiplyPoint3x4(
                        new Vector3(localX, heights[z, x] * size.y, localZ));
                    vertices[z * resolution + x] = ToLVector3(world);
                }
            }

            int writeIndex = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int p00 = z * resolution + x;
                    int p10 = p00 + 1;
                    int p01 = p00 + resolution;
                    int p11 = p01 + 1;
                    indices[writeIndex++] = p00;
                    indices[writeIndex++] = p01;
                    indices[writeIndex++] = p11;
                    indices[writeIndex++] = p00;
                    indices[writeIndex++] = p11;
                    indices[writeIndex++] = p10;
                }
            }

            return MeshCollision3D.NewWorldSpaceOwned(vertices, indices);
        }

        /// <summary>取得列表数量，null 按空列表处理。</summary>
        private static int GetCount<T>(IList<T> source)
        {
            return source == null ? 0 : source.Count;
        }

        private static LVector3 ToLVector3(Vector3 value)
        {
            return LVector3.CreateFromRaw(
                (long)(value.x * LFloat.Precision),
                (long)(value.y * LFloat.Precision),
                (long)(value.z * LFloat.Precision));
        }
    }
}
