using System;

namespace Lockstep.Collision
{
    /// <summary>
    /// 由三角形列表组成的三维网格碰撞体。
    /// vertices 保存不可变局部顶点，worldVertices 是随变换刷新的世界缓存，
    /// triangles 每三个索引构成一个特征面。仅支持 Collision3D 的统一 scale。
    /// </summary>
    public class MeshCollision3D : Collision3D<MeshCollision3D>
    {
        private LVector3[] vertices;
        private bool worldVerticesShareSource;
        private bool worldCacheValid;
        private LVector3 cachedPos;
        private LQuaternion cachedRotation;
        private LFloat cachedScale;

        public LVector3[] worldVertices { get; private set; }
        public int[] triangles { get; private set; }
        public int triangleCount => triangles == null ? 0 : triangles.Length / 3;

        public static MeshCollision3D New(LVector3 pos, LVector3[] vertices, int[] triangles)
        {
            return New(pos, vertices, triangles, LQuaternion.identity);
        }

        public static MeshCollision3D New(
            LVector3 pos,
            LVector3[] vertices,
            int[] triangles,
            LQuaternion rotation)
        {
            Validate(vertices, triangles);

            var mesh = New();
            mesh.vertices = (LVector3[])vertices.Clone();
            mesh.triangles = (int[])triangles.Clone();
            mesh.worldVertices = new LVector3[vertices.Length];
            mesh.worldVerticesShareSource = false;
            mesh.worldCacheValid = false;
            mesh.Init(pos, rotation, LFloat.one);
            return mesh;
        }

        /// <summary>
        /// 接管调用方创建的世界空间顶点和索引数组，不再复制数组，也不再创建第二份世界顶点缓存。
        /// <para>
        /// 本入口用于资源导入、离线导航等已经把顶点烘焙到世界空间的一次性转换流程。
        /// 调用成功后，数组所有权转交给碰撞体；调用方不得再修改数组，并应在使用结束后调用
        /// <see cref="Cycle"/>。碰撞体初始变换固定为原点、单位旋转和单位缩放。
        /// </para>
        /// <para>
        /// 如果之后修改碰撞体变换，首次 <see cref="CalcBounds"/> 会自动把世界缓存与源数组分离，
        /// 不会覆盖接管的局部顶点。该分支只在变换真正变化时产生一次数组分配。
        /// </para>
        /// </summary>
        public static MeshCollision3D NewWorldSpaceOwned(
            LVector3[] worldVertices,
            int[] triangles)
        {
            Validate(worldVertices, triangles);

            var mesh = New();
            mesh.vertices = worldVertices;
            mesh.triangles = triangles;
            mesh.worldVertices = worldVertices;
            mesh.worldVerticesShareSource = true;
            mesh.worldCacheValid = false;
            mesh.Init(LVector3.zero, LQuaternion.identity, LFloat.one);
            return mesh;
        }

        public override bool SetRadius(LFloat radius) => false;
        public override bool SetSize(LVector3 size) => false;

        /// <summary>变换全部局部顶点到世界空间，同时在单次遍历中重建 AABB。</summary>
        public override void CalcBounds()
        {
            if (vertices == null || vertices.Length == 0)
            {
                bounds = new LBounds(pos, pos);
                CacheTransform();
                return;
            }

            // Init 已经计算过当前变换时，NavBuilder 无需再次遍历全部顶点。
            // Collision3D 的变换只能通过 Setter 修改，因此三项相等即可证明世界缓存仍然有效。
            if (worldCacheValid &&
                cachedPos == pos &&
                cachedRotation == rotation &&
                cachedScale == scale)
                return;

            bool identityTransform =
                pos == LVector3.zero &&
                rotation == LQuaternion.identity &&
                scale == LFloat.one;

            // NewWorldSpaceOwned 初始时允许局部数组与世界数组共享；一旦增加变换必须先分离，
            // 否则原地写入会破坏后续重新计算所需的局部顶点。
            if (worldVertices == null ||
                worldVertices.Length != vertices.Length ||
                (worldVerticesShareSource && !identityTransform))
            {
                worldVertices = new LVector3[vertices.Length];
                worldVerticesShareSource = false;
            }

            LVector3 first = identityTransform ? vertices[0] : TransformPoint(vertices[0]);
            worldVertices[0] = first;
            var min = first;
            var max = first;
            for (int i = 1; i < vertices.Length; i++)
            {
                LVector3 point = identityTransform ? vertices[i] : TransformPoint(vertices[i]);
                worldVertices[i] = point;
                min = new LVector3(
                    LMath.Min(min.x, point.x),
                    LMath.Min(min.y, point.y),
                    LMath.Min(min.z, point.z));
                max = new LVector3(
                    LMath.Max(max.x, point.x),
                    LMath.Max(max.y, point.y),
                    LMath.Max(max.z, point.z));
            }
            bounds = new LBounds(min, max);
            CacheTransform();
        }

        public override void Cycle()
        {
            vertices = null;
            worldVertices = null;
            triangles = null;
            worldVerticesShareSource = false;
            worldCacheValid = false;
            cachedPos = default;
            cachedRotation = default;
            cachedScale = default;
            base.Cycle();
        }

        /// <summary>记录生成世界顶点和 AABB 时使用的变换，用于跳过没有变化的重复计算。</summary>
        private void CacheTransform()
        {
            cachedPos = pos;
            cachedRotation = rotation;
            cachedScale = scale;
            worldCacheValid = true;
        }

        /// <summary>确保顶点、三角形数组有效且每个索引都在顶点范围内。</summary>
        private static void Validate(LVector3[] vertices, int[] triangles)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (triangles == null)
                throw new ArgumentNullException(nameof(triangles));
            if (triangles.Length % 3 != 0)
                throw new ArgumentException("Triangle indices must be provided in groups of three.", nameof(triangles));

            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= vertices.Length)
                    throw new ArgumentOutOfRangeException(nameof(triangles), "Triangle index is outside the vertex array.");
            }
        }
    }
}
