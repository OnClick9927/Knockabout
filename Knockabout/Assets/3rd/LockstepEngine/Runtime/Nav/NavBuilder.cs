using System;
using System.Collections.Generic;
using Lockstep.Collision;

namespace Lockstep.Nav
{
    /// <summary>
    /// 与 Unity 完全无关的导航构建入口。
    /// <para>
    /// 本类不引用 UnityEditor、UnityEngine 或 Unity Navigation。外部先完成对象、层级、标签等
    /// 业务筛选，再把跳跃链接和 Lockstep 三维碰撞体交给本类，因此同一套构建逻辑可以运行在
    /// Unity 客户端、独立服务器和命令行离线工具中。
    /// </para>
    /// <para>
    /// 本类不会持有、回收或修改碰撞体的生命周期。输入转换完成后，后续高度场、区域简化、
    /// 约束德洛内三角剖分、邻接和离线链接都由 <see cref="NavMeshBuilder"/> 完成。
    /// </para>
    /// </summary>
    public static class NavBuilder
    {
        /// <summary>
        /// 根据导航设置、跳跃链接、障碍碰撞体和可行走碰撞体生成导航数据。
        /// 四组输入都应由外部提前完成业务筛选；本方法不会查询场景、组件、层级或标签。
        /// <paramref name="links"/> 不参与几何收集，会在导航三角形生成后原样交给链接构建阶段。
        /// <paramref name="obstacles"/> 会作为实体参与占用和净空计算，但其表面绝不会生成行走面；
        /// <paramref name="walks"/> 的表面则会继续接受坡度、净空、步高和区域大小筛选。
        /// </summary>
        /// <remarks>
        /// 当前精确支持 <see cref="MeshCollision3D"/> 和 <see cref="BoxCollision3D"/>。
        /// 球体、胶囊体以及未知的自定义碰撞体没有唯一的有限三角形表达，必须由调用方先按所需精度
        /// 离散为 <see cref="NavBuildTriangle"/>，避免这里偷偷使用 AABB 或固定细分精度改变导航边界。
        /// </remarks>
        public static NavData Build(
            NavBuildSettings settings,
            IList<NavBuildLink> links,
            IList<Collision3D> obstacles,
            IList<Collision3D> walks,
            out NavBuildReport report)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // 所有构建状态均属于本次调用，不保存在静态字段中。这样 Build 可自然并发、重入，
            // 也不会让长期存活的缓存集合持有大数组或外部碰撞体引用。
            var triangles = new List<NavBuildTriangle>();
            var obstacleSet = new HashSet<Collision3D>();
            var converted = new HashSet<Collision3D>();

            EnsureTriangleCapacity(triangles, obstacles, walks);
            CollectGeometry(
                obstacles,
                walks,
                triangles,
                obstacleSet,
                converted);
            return NavMeshBuilder.Build(triangles, settings, links, out report);
        }

        /// <summary>
        /// 不需要读取构建统计时的便捷入口。
        /// 参数顺序固定为：导航设置、跳跃链接、不可行走碰撞体、可行走碰撞体。
        /// </summary>
        public static NavData Build(
            NavBuildSettings settings,
            IList<NavBuildLink> links,
            IList<Collision3D> obstacles,
            IList<Collision3D> walks)
        {
            return Build(settings, links, obstacles, walks, out _);
        }

        /// <summary>
        /// 汇总外部已经筛选好的障碍与可行走碰撞体，并生成导航构建所需的世界空间三角形。
        /// <para>该方法只供 <see cref="Build(NavBuildSettings,IList{NavBuildLink},IList{Collision3D},IList{Collision3D},out NavBuildReport)"/>
        /// 使用，不对外暴露。它只复制碰撞体当前几何，不保存引用，也不负责碰撞体的创建或回收。</para>
        /// <para>同一个碰撞体同时出现在两组时以障碍组为准；同组重复项只展开一次。</para>
        /// </summary>
        private static void CollectGeometry(
            IList<Collision3D> obstacles,
            IList<Collision3D> walks,
            List<NavBuildTriangle> result,
            HashSet<Collision3D> obstacleSet,
            HashSet<Collision3D> converted)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (obstacleSet == null) throw new ArgumentNullException(nameof(obstacleSet));
            if (converted == null) throw new ArgumentNullException(nameof(converted));

            FillCollisionSet(obstacles, nameof(obstacles), obstacleSet);

            // 先展开可行走组，但跳过同时被声明为障碍的对象，让障碍语义拥有更高优先级。
            AppendGeometry(
                walks,
                nameof(walks),
                false,
                obstacleSet,
                converted,
                result);
            AppendGeometry(
                obstacles,
                nameof(obstacles),
                true,
                null,
                converted,
                result);
        }

        /// <summary>
        /// 校验碰撞体列表并建立引用集合。集合只用于判定障碍优先级，不会改变调用方列表。
        /// </summary>
        private static void FillCollisionSet(
            IList<Collision3D> collisions,
            string parameterName,
            HashSet<Collision3D> result)
        {
            if (collisions == null) return;

            for (int i = 0; i < collisions.Count; i++)
            {
                Collision3D collision = collisions[i];
                if (collision == null)
                    throw new ArgumentException(
                        $"Collision at index {i} is null.",
                        parameterName);
                result.Add(collision);
            }
        }

        /// <summary>
        /// 根据碰撞体类型预估三角形上限并一次性扩容。重复碰撞体会让估算偏大，但不会影响结果；
        /// 相比 List 在数万三角形时多次翻倍、复制底层数组，这次轻量遍历的 CPU 成本更低且稳定。
        /// </summary>
        private static void EnsureTriangleCapacity(
            List<NavBuildTriangle> triangles,
            IList<Collision3D> obstacles,
            IList<Collision3D> walks)
        {
            long required = EstimateTriangleCount(obstacles) + EstimateTriangleCount(walks);
            if (required <= triangles.Capacity) return;

            triangles.Capacity = required >= int.MaxValue
                ? int.MaxValue
                : (int)required;
        }

        /// <summary>只读取形状公开计数，不计算 Bounds，也不分配临时对象。</summary>
        private static long EstimateTriangleCount(IList<Collision3D> collisions)
        {
            if (collisions == null) return 0L;

            long count = 0L;
            for (int i = 0; i < collisions.Count; i++)
            {
                Collision3D collision = collisions[i];
                if (collision is MeshCollision3D mesh)
                    count += mesh.triangleCount;
                else if (collision is BoxCollision3D)
                    count += 12L;

                if (count >= int.MaxValue) return int.MaxValue;
            }
            return count;
        }

        /// <summary>
        /// 按列表顺序转换碰撞体。skip 中的对象由更高优先级分组负责转换；converted 用于阻止重复输入。
        /// </summary>
        private static void AppendGeometry(
            IList<Collision3D> geometry,
            string parameterName,
            bool blockWalkableSurface,
            HashSet<Collision3D> skip,
            HashSet<Collision3D> converted,
            List<NavBuildTriangle> result)
        {
            if (geometry == null) return;

            for (int i = 0; i < geometry.Count; i++)
            {
                Collision3D collision = geometry[i];
                if (collision == null)
                    throw new ArgumentException(
                        $"Collision at index {i} is null.",
                        parameterName);
                if (skip != null && skip.Contains(collision)) continue;
                if (!converted.Add(collision)) continue;

                if (collision is MeshCollision3D mesh)
                {
                    AddMesh(mesh, blockWalkableSurface, result);
                    continue;
                }

                if (collision is BoxCollision3D box)
                {
                    AddBox(box, blockWalkableSurface, result);
                    continue;
                }

                throw new NotSupportedException(
                    $"Navigation geometry cannot exactly triangulate {collision.GetType().FullName}. " +
                    $"Convert it to {nameof(NavBuildTriangle)} data before building.");
            }
        }

        /// <summary>
        /// 展开网格碰撞体的索引。调用 <see cref="Collision3D.CalcBounds"/> 会同步刷新
        /// <see cref="MeshCollision3D.worldVertices"/>，保证刚修改过变换的网格也按最新位置导出。
        /// </summary>
        private static void AddMesh(
            MeshCollision3D mesh,
            bool blockWalkableSurface,
            List<NavBuildTriangle> result)
        {
            mesh.CalcBounds();
            LVector3[] vertices = mesh.worldVertices;
            int[] indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                result.Add(new NavBuildTriangle(
                    vertices[indices[i]],
                    vertices[indices[i + 1]],
                    vertices[indices[i + 2]],
                    blockWalkableSurface));
            }
        }

        /// <summary>
        /// 把有向包围盒精确展开为八个角点和十二个三角形。
        /// 顶点直接使用碰撞体公开的定点半轴计算，不经过浮点矩阵，因此不同平台得到相同结果。
        /// </summary>
        private static void AddBox(
            BoxCollision3D box,
            bool blockWalkableSurface,
            List<NavBuildTriangle> result)
        {
            LVector3 halfSize = box.halfSize;
            LVector3 x = box.axisX * halfSize.x;
            LVector3 y = box.axisY * halfSize.y;
            LVector3 z = box.axisZ * halfSize.z;
            LVector3 center = box.pos;

            // 八个角点使用局部值类型变量保存，消除每个 Box 原有的顶点数组与索引数组分配。
            LVector3 v0 = center - x - y - z;
            LVector3 v1 = center + x - y - z;
            LVector3 v2 = center + x - y + z;
            LVector3 v3 = center - x - y + z;
            LVector3 v4 = center - x + y - z;
            LVector3 v5 = center + x + y - z;
            LVector3 v6 = center + x + y + z;
            LVector3 v7 = center - x + y + z;

            // 每个面的向外绕序与旧索引表完全一致，保证生成结果和确定性排序不变。
            AddQuad(result, v0, v1, v2, v3, blockWalkableSurface); // 下
            AddTriangle(result, v4, v6, v5, blockWalkableSurface); // 上 1
            AddTriangle(result, v4, v7, v6, blockWalkableSurface); // 上 2
            AddQuad(result, v0, v4, v5, v1, blockWalkableSurface); // 后
            AddQuad(result, v1, v5, v6, v2, blockWalkableSurface); // 右
            AddQuad(result, v3, v2, v6, v7, blockWalkableSurface); // 前
            AddQuad(result, v0, v3, v7, v4, blockWalkableSurface); // 左
        }

        /// <summary>按 a-b-c、a-c-d 的固定绕序把四边形展开为两个导航三角形。</summary>
        private static void AddQuad(
            List<NavBuildTriangle> result,
            LVector3 a,
            LVector3 b,
            LVector3 c,
            LVector3 d,
            bool blockWalkableSurface)
        {
            AddTriangle(result, a, b, c, blockWalkableSurface);
            AddTriangle(result, a, c, d, blockWalkableSurface);
        }

        /// <summary>追加一个导航三角形，集中保持 Box 展开字段赋值方式一致。</summary>
        private static void AddTriangle(
            List<NavBuildTriangle> result,
            LVector3 a,
            LVector3 b,
            LVector3 c,
            bool blockWalkableSurface)
        {
            result.Add(new NavBuildTriangle(a, b, c, blockWalkableSurface));
        }
    }
}
