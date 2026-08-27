using System;
using System.Collections.Generic;
using Lockstep;
using Lockstep.Nav;
using UnityEngine;

namespace LockstepExamples.NavRvoDemo
{
    /// <summary>
    /// 指定普通序列化字段在自定义 Inspector 中显示的中文名称。
    /// Unity 自带的 <see cref="InspectorNameAttribute"/> 只保证枚举项改名，因此示例使用独立特性，
    /// 由 Editor 程序集读取后替换字段标签；运行时构建不会引用 UnityEditor。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NavInspectorLabelAttribute : Attribute
    {
        /// <summary>Inspector 中显示的完整字段名称。</summary>
        public string DisplayName { get; }

        public NavInspectorLabelAttribute(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Inspector 显示名称不能为空。", nameof(displayName));

            DisplayName = displayName;
        }
    }

    /// <summary>
    /// NavMap 全局寻路与 RVO 局部避障的可运行示例。
    /// <para>
    /// 场景进入 Play Mode 后，本组件会创建一块带有五个障碍岛和多段回折通道的纯 Lockstep NavMesh，
    /// 并让多名代理沿同向环线持续行走、按 Inspector 配置的间隔向前推进一次目的地。可行走表面由确定性的多频波形生成连续起伏，
    /// 代理之间由 RVO/ORCA 跟随、避让；每次 RVO 积分后的坐标都会由 NavRvoWorld 约束回导航
    /// 三角形，所以代理中心始终贴合崎岖地面，也不会进入障碍孔洞或越过外边界。
    /// </para>
    /// <para>
    /// Unity Primitive、Mesh 和材质只承担显示职责。示例不创建 NavMeshSurface、NavMeshAgent、
    /// Unity Collider，也不调用 Unity Physics 或 Unity Navigation。
    /// </para>
    /// </summary>
    public sealed class NavRvoExample : MonoBehaviour
    {
        private sealed class AgentView
        {
            public NavRvoAgent agent;
            public Transform transform;
            public Renderer renderer;
            public int nextRouteIndex;
            public int colorIndex;
            public LFloat lowSpeedElapsed;
        }

        /// <summary>使用原始定点坐标组成无向边键，用于建立示例三角形的普通邻接。</summary>
        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            private readonly long ax;
            private readonly long az;
            private readonly long bx;
            private readonly long bz;

            public EdgeKey(LVector3 first, LVector3 second)
            {
                bool ordered = first._x < second._x ||
                               first._x == second._x && first._z <= second._z;
                LVector3 a = ordered ? first : second;
                LVector3 b = ordered ? second : first;
                ax = a._x;
                az = a._z;
                bx = b._x;
                bz = b._z;
            }

            public bool Equals(EdgeKey other)
            {
                return ax == other.ax && az == other.az &&
                       bx == other.bx && bz == other.bz;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ax.GetHashCode();
                    hash = hash * 397 ^ az.GetHashCode();
                    hash = hash * 397 ^ bx.GetHashCode();
                    return hash * 397 ^ bz.GetHashCode();
                }
            }
        }

        private readonly struct EdgeOwner
        {
            public readonly int triangleIndex;

            public EdgeOwner(int triangleIndex)
            {
                this.triangleIndex = triangleIndex;
            }
        }

        /// <summary>
        /// 一个轴对齐的地形障碍区域。整数边界用于剔除导航网格单元，显示体和 RVO 实体轮廓
        /// 使用相同的固定内缩；RVO 在求解时会再按代理半径膨胀实体轮廓。普通路径拐点则按代理
        /// 半径判定通过，避免要求圆形代理的中心精确触碰孔洞直角。
        /// </summary>
        private readonly struct ObstacleArea
        {
            public readonly int minX;
            public readonly int maxX;
            public readonly int minZ;
            public readonly int maxZ;
            public readonly float height;

            public ObstacleArea(int minX, int maxX, int minZ, int maxZ, float height)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minZ = minZ;
                this.maxZ = maxZ;
                this.height = height;
            }

            /// <summary>判断以整数坐标为左下角的导航单元是否落在当前孔洞内。</summary>
            public bool ContainsCell(int x, int z)
            {
                return x >= minX && x < maxX && z >= minZ && z < maxZ;
            }
        }

        [Header("固定帧与 Agent")]
        [Tooltip("每个 Unity FixedUpdate 推进的 Lockstep 固定时间。运行中修改会在下一逻辑帧生效。")]
        [SerializeField, Min(0.01f), NavInspectorLabel("模拟步长（秒）")]
        private float simulationTimeStep = 0.05f;

        [Tooltip("Agent 的基础最大移动速度。每个 Agent 还会乘以下方的速度差异倍率。重新进入 Play Mode 后生效。")]
        [SerializeField, Min(0f), NavInspectorLabel("基础最大速度")]
        private float maxSpeed = 0.8f;

        [Tooltip("RVO 圆形 Agent 的半径，同时用于路径拐点通过范围和显示球大小。重新进入 Play Mode 后生效。")]
        [SerializeField, Min(0.01f), NavInspectorLabel("Agent 半径")]
        private float agentRadius = 0.32f;

        [Tooltip("沿环线均匀生成的 Agent 数量。数量越大，窄通道中的排队和避让越明显。重新进入 Play Mode 后生效。")]
        [SerializeField, Range(1, 18), NavInspectorLabel("Agent 数量")]
        private int agentCount = 12;

        [Tooltip("最慢 Agent 相对基础最大速度的倍率；最快档始终为 1。")]
        [SerializeField, Range(0.1f, 1f), NavInspectorLabel("最低速度倍率")]
        private float minimumSpeedScale = 0.82f;

        [Tooltip("从最低速度倍率到 1 之间均匀划分的速度档位数量，用于避免所有 Agent 完全同速并排。")]
        [SerializeField, Range(1, 8), NavInspectorLabel("速度档位数量")]
        private int speedVariantCount = 4;

        [Tooltip("所有 Agent 沿同向环线向前推进一次目标的固定模拟时间间隔。")]
        [SerializeField, Min(0.1f), NavInspectorLabel("换目标间隔（秒）")]
        private float destinationChangeInterval = 20f;

        [Tooltip("确定性随机地形种子；修改后重新进入 Play Mode 会生成另一套起伏。")]
        [SerializeField, NavInspectorLabel("地形随机种子")]
        private int terrainSeed = 20260813;

        [Header("RVO 近邻与预测")]
        [Tooltip("KD 树搜索其他 Agent 的最大距离。过小会太晚发现来车，过大会增加近邻查询范围。")]
        [SerializeField, Min(0f), NavInspectorLabel("邻居搜索距离")]
        private float neighborDistance = 4f;

        [Tooltip("一次 ORCA 求解最多考虑的移动 Agent 数量。零表示不考虑其他移动 Agent。")]
        [SerializeField, Range(0, 32), NavInspectorLabel("最大邻居数量")]
        private int maxNeighbors = 8;

        [Tooltip("预测移动 Agent 碰撞的时间范围。数值越大越早避让，但密集区域也会更保守。")]
        [SerializeField, Min(0.01f), NavInspectorLabel("Agent 预测时间（秒）")]
        private float timeHorizon = 1.2f;

        [Tooltip("预测静态 RVO 障碍碰撞的时间范围，可以与移动 Agent 的预测时间分别调节。")]
        [SerializeField, Min(0.01f), NavInspectorLabel("障碍预测时间（秒）")]
        private float obstacleTimeHorizon = 1.2f;

        [Header("拥堵检测与排队")]
        [Tooltip("沿路径方向缺少有效前进达到该时间后，Agent 进入持续拥堵状态。零表示首个低进展帧立即触发。")]
        [SerializeField, Min(0f), NavInspectorLabel("拥堵判定时间（秒）")]
        private float congestionDetectionTime = 0.25f;

        [Tooltip("沿路径方向的速度低于最大速度乘以该比例时累计拥堵时间。推荐 0.01 到 0.2。")]
        [SerializeField, Range(0f, 1f), NavInspectorLabel("低进展速度比例")]
        private float congestionForwardSpeedRatio = 0.05f;

        [Tooltip("局部拥堵组半径相对双方组合半径的倍率。越大越早组成排队组，零近似关闭组队。")]
        [SerializeField, Min(0f), NavInspectorLabel("拥堵组半径倍率")]
        private float congestionGroupRadiusScale = 3f;

        [Tooltip("预测双方原始期望轨迹是否相交的最长时间。零表示不预测未来交点。")]
        [SerializeField, Min(0f), NavInspectorLabel("冲突预测时间（秒）")]
        private float congestionPredictionTime = 2f;

        [Tooltip("在双方半径之和外，额外加入最大 Agent 半径乘以该比例作为预测冲突余量。")]
        [SerializeField, Min(0f), NavInspectorLabel("预测冲突余量")]
        private float congestionConflictMargin = 0.5f;

        [Tooltip("验证侧移和退让方向时向前预览的逻辑步数。数值越大越不容易贴近 NavMesh 边界。")]
        [SerializeField, Range(0, 16), NavInspectorLabel("方向探针步数")]
        private int congestionProbeSteps = 4;

        [Tooltip("预测冲突或持续拥堵时，向行进方向右侧附加的期望速度比例。零表示关闭靠右引导。")]
        [SerializeField, Range(0f, 1f), NavInspectorLabel("靠右引导比例")]
        private float congestionSideBias = 0.35f;

        [Tooltip("拥堵组中非优先 Agent 主动退让的最大速度比例。零表示关闭排队退让。")]
        [SerializeField, Range(0f, 1f), NavInspectorLabel("排队退让速度比例")]
        private float congestionYieldSpeed = 0.45f;

        [Tooltip("侧向引导和退让方向至少保持的时间，避免刚产生位移就重新落入对称死锁。")]
        [SerializeField, Min(0f), NavInspectorLabel("拥堵引导保持时间（秒）")]
        private float congestionBiasDuration = 1f;

        [Header("路径与停滞恢复")]
        [Tooltip("路径点在 XZ 平面上的额外到达容差；普通拐点还会自动加上 Agent 半径。")]
        [SerializeField, Min(0f), NavInspectorLabel("路径点容差")]
        private float waypointTolerance = 0.08f;

        [Tooltip("RVO 候选位置被 NavMesh 边界裁剪后，是否从合法位置自动重新搜索当前目标。")]
        [SerializeField, NavInspectorLabel("边界裁剪后重新寻路")]
        private bool repathWhenConstrained = true;

        [Tooltip("是否自动跨越 NavMap 离散 Link。示例当前没有 Link，保留该项用于验证完整 Agent 配置。")]
        [SerializeField, NavInspectorLabel("自动跨越 Link")]
        private bool autoTraverseLinks = true;

        [Tooltip("示例层判断 Agent 近似静止的实际速度阈值；只用于重新提交当前路线段，不参与运行时拥堵分组。")]
        [SerializeField, Min(0f), NavInspectorLabel("停滞速度阈值")]
        private float stalledSpeedThreshold = 0.01f;

        [Tooltip("实际速度连续低于停滞阈值达到该时间后，示例重新提交当前同向路线段。")]
        [SerializeField, Min(0f), NavInspectorLabel("停滞恢复时间（秒）")]
        private float stalledRecoveryTime = 2f;

        [Header("调试显示")]
        [Tooltip("用青色线框显示代码生成的 Lockstep 导航三角形。")]
        [SerializeField, NavInspectorLabel("显示导航网格")]
        private bool drawNavMesh = true;

        [Tooltip("显示每个 Agent 当前的 NavMap 路径和最终目标。")]
        [SerializeField, NavInspectorLabel("显示路径")]
        private bool drawPaths = true;

        [Tooltip("显示 RVO 在上一个逻辑帧实际采用的速度方向。")]
        [SerializeField, NavInspectorLabel("显示实际速度")]
        private bool drawVelocities = true;

        private static readonly Color[] AgentColors =
        {
            new Color(0.12f, 0.7f, 0.82f, 1f),
            new Color(0.98f, 0.48f, 0.16f, 1f),
            new Color(0.36f, 0.8f, 0.34f, 1f),
            new Color(0.9f, 0.25f, 0.58f, 1f),
            new Color(0.96f, 0.78f, 0.18f, 1f),
            new Color(0.54f, 0.42f, 0.92f, 1f)
        };

        // 五个不规则分布的矩形孔洞共同组成回折地形。边界采用整数，保证孔洞恰好沿网格边缘切开，
        // 邻接构建时不会产生只重叠一部分的边，也不需要额外执行浮点几何修补。
        private static readonly ObstacleArea[] ObstacleAreas =
        {
            new ObstacleArea(-9, -4, -8, -4, 1.1f),
            new ObstacleArea(-9, -4, 1, 7, 1.8f),
            new ObstacleArea(-1, 2, -2, 2, 1.35f),
            new ObstacleArea(4, 9, -7, -1, 1.55f),
            new ObstacleArea(4, 9, 4, 8, 1.2f)
        };

        private const int NavMin = -15;
        private const int NavMax = 15;
        private const long ObstacleInsetRaw = 400000L;
        private const long TerrainAmplitudeRaw = 1800000L;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<AgentView> agentViews = new List<AgentView>();
        private readonly List<LVector3> routePoints = new List<LVector3>();
        private  MaterialPropertyBlock propertyBlock;
        private NavRvoWorld world;
        private NavData navData;
        private Transform generatedRoot;
        private Mesh navMeshVisual;
        private Material surfaceMaterial;
        private Material agentMaterial;
        private Material obstacleMaterial;
        private LFloat destinationElapsed;
        private LFloat destinationChangePeriod;

        private void Start()
        {
            propertyBlock = new MaterialPropertyBlock();
            CreateCamera();
            CreateLight();
            CreateMaterials();

            generatedRoot = new GameObject("Generated Nav/RVO Example").transform;
            generatedRoot.SetParent(transform, false);

            navData = CreateComplexNavData();
            world = new NavRvoWorld(
                navData,
                LMath.ToLFloat(Mathf.Max(0.01f, simulationTimeStep)));
            destinationElapsed = LFloat.zero;
            destinationChangePeriod = LMath.ToLFloat(Mathf.Max(0.1f, destinationChangeInterval));

            // NavMesh 约束负责最终位置安全；同一组孔洞轮廓也交给 RVO 后，ORCA 会在速度求解阶段
            // 提前绕开边缘，而不是等积分位置触碰边界后再由 NavMesh 裁回。
            AddRvoObstacles();
            world.ProcessObstacles();

            CreateNavMeshVisual();
            CreateObstacleVisuals();
            CreateAgents();
        }

        private void FixedUpdate()
        {
            if (world == null) return;

            // Unity 的 FixedUpdate 仅负责触发示例逻辑帧；权威位置和速度仍全部是定点值。
            LFloat timeStep = LMath.ToLFloat(Mathf.Max(0.01f, simulationTimeStep));
            world.TimeStep = timeStep;

            // 使用模拟步长而不是 Unity Time.time 计时，因此暂停、帧率波动或重放都不会改变换目标时刻。
            // 每满配置的换目标间隔，把所有代理的目的地沿环线向前推进一段。代理若提前到达当前路线点，
            // KeepAgentMoving 会立即接续下一段，避免为了等待统一调度而变成静止障碍。
            destinationChangePeriod = LMath.ToLFloat(Mathf.Max(0.1f, destinationChangeInterval));
            destinationElapsed += timeStep;
            while (destinationElapsed >= destinationChangePeriod)
            {
                destinationElapsed -= destinationChangePeriod;
                ChangeAllDestinations();
            }

            world.Step();

            // 示例层的停滞阈值对本帧所有 Agent 相同，只转换一次，避免在循环中重复执行浮点到定点转换。
            LFloat recoverySpeedThreshold =
                LMath.ToLFloat(Mathf.Max(0f, stalledSpeedThreshold));
            LFloat recoveryDuration =
                LMath.ToLFloat(Mathf.Max(0f, stalledRecoveryTime));
            for (int i = 0; i < agentViews.Count; i++)
            {
                AgentView view = agentViews[i];
                // RVO 提交后立即处理本帧产生的抵达、寻路失败或持续低速，渲染阶段不会保留停止状态。
                KeepAgentMoving(
                    view,
                    timeStep,
                    recoverySpeedThreshold,
                    recoveryDuration);
                LVector3 position = view.agent.Position;
                view.transform.position = position.ToVector3() + Vector3.up * agentRadius;

                LVector2 velocity = view.agent.Velocity;
                if (velocity.sqrMagnitude > LFloat.EPSILON)
                {
                    Vector3 direction = new Vector3(
                        velocity.x.ToFloat(), 0f, velocity.y.ToFloat());
                    if (direction.sqrMagnitude > Mathf.Epsilon)
                        view.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }

                UpdateAgentColor(view);
            }
        }

        /// <summary>创建水平网格并剔除五组障碍单元，形成包含窄通道和多次转向的复杂导航区域。</summary>
        private NavData CreateComplexNavData()
        {
            var triangles = new List<Triangle>((NavMax - NavMin) * (NavMax - NavMin) * 2);

            for (int z = NavMin; z < NavMax; z++)
            {
                for (int x = NavMin; x < NavMax; x++)
                {
                    if (IsObstacleCell(x, z))
                        continue;

                    // 相邻单元通过同一个整数顶点函数取高，因此共享边的两个端点完全一致；
                    // 地面虽然崎岖，仍然是一张连续且可以稳定建立邻接的三角曲面。
                    LVector3 p00 = CreateTerrainVertex(x, z);
                    LVector3 p10 = CreateTerrainVertex(x + 1, z);
                    LVector3 p11 = CreateTerrainVertex(x + 1, z + 1);
                    LVector3 p01 = CreateTerrainVertex(x, z + 1);
                    triangles.Add(CreateTriangle(p00, p11, p10));
                    triangles.Add(CreateTriangle(p00, p01, p11));
                }
            }

            ConnectTriangleNeighbors(triangles);
            return new NavData
            {
                agentType = 0,
                triangles = triangles
            };
        }

        /// <summary>
        /// 从整数坐标和种子生成 [-1, 1] 的确定性伪随机值。这里只使用无符号整数的溢出、异或和乘法，
        /// 不读取 UnityEngine.Random，因此相同种子在不同平台和重放中都会得到相同高度。
        /// </summary>
        private static long GetNoiseRaw(int x, int z, int seed)
        {
            unchecked
            {
                uint hash = (uint)seed;
                hash ^= (uint)x * 0x9E3779B9u;
                hash ^= (uint)z * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) * 2000000L / 0x00FFFFFFu - 1000000L;
            }
        }

        /// <summary>
        /// 对当前顶点及周围八个伪随机样本做加权平滑。中心、十字邻居、对角邻居的权重分别为
        /// 8、2、1，既消除逐格白噪声形成的尖刺，又保留比周期波形更自然、不重复的随机丘陵。
        /// </summary>
        private LFloat GetTerrainHeight(int x, int z)
        {
            long weighted = GetNoiseRaw(x, z, terrainSeed) * 8L;
            weighted += GetNoiseRaw(x - 1, z, terrainSeed) * 2L;
            weighted += GetNoiseRaw(x + 1, z, terrainSeed) * 2L;
            weighted += GetNoiseRaw(x, z - 1, terrainSeed) * 2L;
            weighted += GetNoiseRaw(x, z + 1, terrainSeed) * 2L;
            weighted += GetNoiseRaw(x - 1, z - 1, terrainSeed);
            weighted += GetNoiseRaw(x + 1, z - 1, terrainSeed);
            weighted += GetNoiseRaw(x - 1, z + 1, terrainSeed);
            weighted += GetNoiseRaw(x + 1, z + 1, terrainSeed);
            return LFloat.FromRaw(weighted * TerrainAmplitudeRaw / (20L * LFloat.Precision));
        }

        /// <summary>创建导航网格整数顶点；Y 只由 XZ 决定，保证所有引用该顶点的三角形严格连续。</summary>
        private LVector3 CreateTerrainVertex(int x, int z)
        {
            return new LVector3((LFloat)x, GetTerrainHeight(x, z), (LFloat)z);
        }

        /// <summary>
        /// 把任意 XZ 路线坐标投影到示例网格的实际三角面。插值分支与每个单元的对角线方向一致，
        /// 避免直接计算连续波形后与分片线性 NavMesh 之间产生高度误差。
        /// </summary>
        private LVector3 CreateTerrainPoint(float x, float z)
        {
            int cellX = Mathf.FloorToInt(x);
            int cellZ = Mathf.FloorToInt(z);
            LFloat fixedX = LFloat.FromRaw((long)(x * LFloat.Precision));
            LFloat fixedZ = LFloat.FromRaw((long)(z * LFloat.Precision));
            LFloat u = fixedX - cellX;
            LFloat v = fixedZ - cellZ;
            LFloat h00 = GetTerrainHeight(cellX, cellZ);
            LFloat h10 = GetTerrainHeight(cellX + 1, cellZ);
            LFloat h11 = GetTerrainHeight(cellX + 1, cellZ + 1);
            LFloat h01 = GetTerrainHeight(cellX, cellZ + 1);

            LFloat height = v <= u
                ? h00 * (LFloat.one - u) + h11 * v + h10 * (u - v)
                : h00 * (LFloat.one - v) + h01 * (v - u) + h11 * u;
            return new LVector3(fixedX, height, fixedZ);
        }

        /// <summary>集中判断孔洞单元，保证导航三角形、RVO 轮廓和可视障碍使用同一份地形配置。</summary>
        private static bool IsObstacleCell(int x, int z)
        {
            for (int i = 0; i < ObstacleAreas.Length; i++)
            {
                if (ObstacleAreas[i].ContainsCell(x, z))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 把每个显示障碍的实体边界注册给 RVO。RVO 会在求解时自行按代理半径膨胀该轮廓；
        /// NavMesh 孔洞继续承担最终的中心位置约束。两者使用同一份实体内缩参数，保证画面中的
        /// 方块和局部避障看到的是同一个障碍大小。
        /// </summary>
        private void AddRvoObstacles()
        {
            long precision = LFloat.Precision;
            for (int i = 0; i < ObstacleAreas.Length; i++)
            {
                ObstacleArea area = ObstacleAreas[i];
                LFloat minX = LFloat.FromRaw(area.minX * precision + ObstacleInsetRaw);
                LFloat maxX = LFloat.FromRaw(area.maxX * precision - ObstacleInsetRaw);
                LFloat minZ = LFloat.FromRaw(area.minZ * precision + ObstacleInsetRaw);
                LFloat maxZ = LFloat.FromRaw(area.maxZ * precision - ObstacleInsetRaw);
                world.AddObstacle(new[]
                {
                    new LVector2(minX, minZ),
                    new LVector2(maxX, minZ),
                    new LVector2(maxX, maxZ),
                    new LVector2(minX, maxZ)
                });
            }
        }

        private static Triangle CreateTriangle(LVector3 a, LVector3 b, LVector3 c)
        {
            var triangle = new Triangle();
            triangle.points[0] = a;
            triangle.points[1] = b;
            triangle.points[2] = c;
            triangle.edges[0] = Edge.Create(a, b);
            triangle.edges[1] = Edge.Create(b, c);
            triangle.edges[2] = Edge.Create(c, a);
            triangle.bounds = new LBounds(a, a);
            triangle.bounds.Encapsulate(b);
            triangle.bounds.Encapsulate(c);
            return triangle;
        }

        /// <summary>按完全相同的共享边连接普通邻居；中央孔洞和外轮廓自然保持无邻居状态。</summary>
        private static void ConnectTriangleNeighbors(List<Triangle> triangles)
        {
            var edgeOwners = new Dictionary<EdgeKey, EdgeOwner>(triangles.Count * 3);
            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                Triangle triangle = triangles[triangleIndex];
                for (int edgeIndex = 0; edgeIndex < triangle.edges.Length; edgeIndex++)
                {
                    Edge edge = triangle.edges[edgeIndex];
                    var key = new EdgeKey(edge.a, edge.b);
                    if (!edgeOwners.TryGetValue(key, out EdgeOwner owner))
                    {
                        edgeOwners.Add(key, new EdgeOwner(triangleIndex));
                        continue;
                    }

                    Triangle other = triangles[owner.triangleIndex];
                    triangle.neighbors.Add(owner.triangleIndex);
                    other.neighbors.Add(triangleIndex);
                }
            }
        }

        private void CreateAgents()
        {
            // 环线从底部外围进入右侧回折走廊，再绕过顶部并进入左侧回折走廊，最终返回起点。
            // 障碍岛会阻止 NavMap 直接抄近路，因此代理必须连续经过宽通道、窄口和多个直角转弯。
            // 所有代理都按下列顺序前进，避免远端最短路把它们从相反方向反复导向同一个瓶颈。
            routePoints.Clear();
            routePoints.Add(CreateTerrainPoint(-14.2f, -13.5f));
            routePoints.Add(CreateTerrainPoint(-3f, -13.5f));
            routePoints.Add(CreateTerrainPoint(3f, -13.5f));
            routePoints.Add(CreateTerrainPoint(14.2f, -13.5f));
            routePoints.Add(CreateTerrainPoint(14.2f, -4f));
            routePoints.Add(CreateTerrainPoint(14.2f, 0.5f));
            routePoints.Add(CreateTerrainPoint(3f, 0.5f));
            routePoints.Add(CreateTerrainPoint(3f, 3f));
            routePoints.Add(CreateTerrainPoint(14.2f, 3f));
            routePoints.Add(CreateTerrainPoint(14.2f, 13.5f));
            routePoints.Add(CreateTerrainPoint(3f, 13.5f));
            routePoints.Add(CreateTerrainPoint(-3f, 13.5f));
            routePoints.Add(CreateTerrainPoint(-14.2f, 13.5f));
            routePoints.Add(CreateTerrainPoint(-14.2f, 4f));
            routePoints.Add(CreateTerrainPoint(-14.2f, -1.5f));
            routePoints.Add(CreateTerrainPoint(-3f, -1.5f));
            routePoints.Add(CreateTerrainPoint(-3f, -3f));
            routePoints.Add(CreateTerrainPoint(-14.2f, -3f));

            int spawnCount = Mathf.Clamp(agentCount, 1, routePoints.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                // 按路线总长度近似均匀取起点，避免多个代理在第一个模拟帧重叠。
                int startIndex = i * routePoints.Count / spawnCount;
                int variantCount = Mathf.Max(1, speedVariantCount);
                int variantIndex = i % variantCount;
                float variantProgress = variantCount > 1
                    ? variantIndex / (variantCount - 1f)
                    : 1f;
                float speedFactor = Mathf.Lerp(
                    Mathf.Clamp(minimumSpeedScale, 0.1f, 1f),
                    1f,
                    variantProgress);
                CreateAgent(startIndex, speedFactor, i);
            }
        }

        private void CreateAgent(
            int startRouteIndex,
            float speedFactor,
            int colorIndex)
        {
            LFloat lockstepMaxSpeed = LMath.ToLFloat(Mathf.Max(0f, maxSpeed * speedFactor));
            var settings = new NavRvoAgentSettings
            {
                neighborDistance = LMath.ToLFloat(Mathf.Max(0f, neighborDistance)),
                maxNeighbors = Mathf.Max(0, maxNeighbors),
                timeHorizon = LMath.ToLFloat(Mathf.Max(0.01f, timeHorizon)),
                obstacleTimeHorizon = LMath.ToLFloat(Mathf.Max(0.01f, obstacleTimeHorizon)),
                radius = LMath.ToLFloat(Mathf.Max(0.01f, agentRadius)),
                maxSpeed = lockstepMaxSpeed,
                congestionDetectionTime = LMath.ToLFloat(Mathf.Max(0f, congestionDetectionTime)),
                congestionForwardSpeedRatio = LMath.ToLFloat(Mathf.Clamp01(congestionForwardSpeedRatio)),
                congestionGroupRadiusScale = LMath.ToLFloat(Mathf.Max(0f, congestionGroupRadiusScale)),
                congestionPredictionTime = LMath.ToLFloat(Mathf.Max(0f, congestionPredictionTime)),
                congestionConflictMargin = LMath.ToLFloat(Mathf.Max(0f, congestionConflictMargin)),
                congestionProbeSteps = Mathf.Max(0, congestionProbeSteps),
                congestionSideBias = LMath.ToLFloat(Mathf.Clamp01(congestionSideBias)),
                congestionYieldSpeed = LMath.ToLFloat(Mathf.Clamp01(congestionYieldSpeed)),
                congestionBiasDuration = LMath.ToLFloat(Mathf.Max(0f, congestionBiasDuration)),
                waypointTolerance = LMath.ToLFloat(Mathf.Max(0f, waypointTolerance)),
                repathWhenConstrained = repathWhenConstrained,
                autoTraverseLinks = autoTraverseLinks
            };

            LVector3 lockstepStart = routePoints[startRouteIndex];
            NavRvoAgent navAgent = world.AddAgent(lockstepStart, settings);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = $"Nav RVO Agent {agentViews.Count + 1}";
            visual.transform.SetParent(generatedRoot, false);
            visual.transform.localScale = Vector3.one * agentRadius * 2f;
            visual.transform.position = navAgent.Position.ToVector3() + Vector3.up * agentRadius;
            DestroyUnityCollider(visual);

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && agentMaterial != null)
                renderer.sharedMaterial = agentMaterial;

            var view = new AgentView
            {
                agent = navAgent,
                transform = visual.transform,
                renderer = renderer,
                nextRouteIndex = startRouteIndex,
                colorIndex = colorIndex
            };
            agentViews.Add(view);
            TryAdvanceRouteDestination(view);
            UpdateAgentColor(view);
        }

        /// <summary>
        /// 把目的地沿预设环线向前推进一个路线点。相邻路线点共同描述唯一的行进方向，避免直接
        /// 选择远端点时，全局最短路从孔洞另一侧抄近路并与其他代理形成相向流。若当前点和下一点
        /// 因外部修改重合，则继续向前查找，直到提交一条真正进入 Moving 的路径。
        /// </summary>
        private bool TryAdvanceRouteDestination(AgentView view)
        {
            if (routePoints.Count == 0)
                return false;

            int candidateIndex = view.nextRouteIndex;
            for (int attempt = 0; attempt < routePoints.Count; attempt++)
            {
                candidateIndex = (candidateIndex + 1) % routePoints.Count;
                if (TryCommitRouteDestination(view, candidateIndex))
                    return true;
            }

            view.nextRouteIndex = -1;
            view.lowSpeedElapsed = LFloat.zero;
            return false;
        }

        /// <summary>
        /// 重新提交当前同向路线点。低速恢复只能重建当前段，不能在拥堵团中跳到另一个远端目标，
        /// 否则定时调度和低速恢复会反复改变穿越方向，使已经形成的队列永远无法排空。
        /// </summary>
        private bool TryResumeRouteDestination(AgentView view)
        {
            if (view.nextRouteIndex >= 0 &&
                view.nextRouteIndex < routePoints.Count &&
                TryCommitRouteDestination(view, view.nextRouteIndex))
                return true;

            return TryAdvanceRouteDestination(view);
        }

        /// <summary>提交指定环线点，并且只在代理确实进入 Moving 后更新路线游标。</summary>
        private bool TryCommitRouteDestination(AgentView view, int candidateIndex)
        {
            NavResult result = view.agent.SetDestination(routePoints[candidateIndex]);
            if (result != NavResult.Success || view.agent.State != NavRvoAgentState.Moving)
                return false;

            view.nextRouteIndex = candidateIndex;
            view.lowSpeedElapsed = LFloat.zero;
            return true;
        }

        /// <summary>每满一个调度周期，把所有代理的目标沿同向环线向前推进一段。</summary>
        private void ChangeAllDestinations()
        {
            for (int i = 0; i < agentViews.Count; i++)
                TryAdvanceRouteDestination(agentViews[i]);
        }

        /// <summary>
        /// 保证示例代理不会因提前抵达或局部确定性死锁永久静止。正常目标仍按配置间隔统一更新；
        /// 只有代理已经结束路径，或 RVO 实际速度连续两秒近似为零时，才重新提交当前同向路线段。
        /// 已抵达当前点时再向前推进一段；短暂减速仍由 RVO 自由处理，不会破坏碰撞避让。
        /// </summary>
        private void KeepAgentMoving(
            AgentView view,
            LFloat timeStep,
            LFloat speedThreshold,
            LFloat recoveryTime)
        {
            bool needsRecovery = view.agent.State != NavRvoAgentState.Moving;
            if (!needsRecovery && view.agent.Velocity.magnitude <= speedThreshold)
            {
                view.lowSpeedElapsed += timeStep;
                needsRecovery = view.lowSpeedElapsed >= recoveryTime;
            }
            else if (!needsRecovery)
            {
                view.lowSpeedElapsed = LFloat.zero;
            }

            if (!needsRecovery) return;

            // NavRvoWorld 正常提交的位置始终属于 NavMesh。低速通常只是局部拥堵，此时若无条件调用
            // TryRecoverToNavMesh，会把代理从合法位置周期性拉回三角形内部，视觉上表现为来回抖动。
            // 只有外部数据被修改或累计误差确实令当前位置无法归属任何三角形时才执行位置恢复；
            // 对仍在 NavMesh 上的代理只重建当前同向路线段，让下一帧从当前位置连续产生期望速度。
            if (!world.Map.TryGetTriangle(
                    view.agent.Position,
                    out Triangle _,
                    out LVector3 _) &&
                !view.agent.TryRecoverToNavMesh())
            {
                view.lowSpeedElapsed = LFloat.zero;
                return;
            }

            if (TryResumeRouteDestination(view))
                return;

            // 首次完整选点失败通常说明当前位置恰好落在退化共享顶点或边界误差带。该分支只处理
            // 已经停止的异常代理，不会对普通拥堵或低速代理执行周期性位置恢复。回到当前三角形的
            // 稳定内部点后无预约重试一次；成功即恢复连续移动，失败则保留状态供下一帧再次诊断。
            if (view.agent.State != NavRvoAgentState.Moving &&
                view.agent.TryRecoverToNavMesh())
                TryResumeRouteDestination(view);
        }

        private void UpdateAgentColor(AgentView view)
        {
            if (view.renderer == null) return;

            Color color = AgentColors[view.colorIndex % AgentColors.Length];
            if (view.agent.State == NavRvoAgentState.PathFailed)
                color = Color.red;
            else if (view.agent.State == NavRvoAgentState.Arrived)
                color = Color.white;

            view.renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            view.renderer.SetPropertyBlock(propertyBlock);
        }

        private void CreateNavMeshVisual()
        {
            var vertices = new Vector3[navData.triangles.Count * 3];
            var indices = new int[vertices.Length];
            int writeIndex = 0;
            for (int i = 0; i < navData.triangles.Count; i++)
            {
                Triangle triangle = navData.triangles[i];
                vertices[writeIndex] = triangle.point1.ToVector3();
                indices[writeIndex] = writeIndex++;
                vertices[writeIndex] = triangle.point2.ToVector3();
                indices[writeIndex] = writeIndex++;
                vertices[writeIndex] = triangle.point3.ToVector3();
                indices[writeIndex] = writeIndex++;
            }

            navMeshVisual = new Mesh { name = "Nav RVO Example Surface" };
            navMeshVisual.vertices = vertices;
            navMeshVisual.triangles = indices;
            navMeshVisual.RecalculateNormals();
            navMeshVisual.RecalculateBounds();

            var surface = new GameObject("Lockstep NavMesh Surface");
            surface.transform.SetParent(generatedRoot, false);
            surface.transform.position = Vector3.down * 0.02f;
            surface.AddComponent<MeshFilter>().sharedMesh = navMeshVisual;
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            if (surfaceMaterial != null)
                renderer.sharedMaterial = surfaceMaterial;
        }

        private void CreateObstacleVisuals()
        {
            const float inset = ObstacleInsetRaw / (float)LFloat.Precision;
            for (int i = 0; i < ObstacleAreas.Length; i++)
            {
                ObstacleArea area = ObstacleAreas[i];
                // 波峰可能出现在孔洞边界中段而不是四角，因此扫描覆盖区域的全部整数顶点，
                // 让显示方块始终从最低地面以下开始，并高于周围所有起伏表面。
                LFloat minTerrainHeight = LFloat.MaxValue;
                LFloat maxTerrainHeight = LFloat.MinValue;
                for (int z = area.minZ; z <= area.maxZ; z++)
                {
                    for (int x = area.minX; x <= area.maxX; x++)
                    {
                        LFloat height = GetTerrainHeight(x, z);
                        minTerrainHeight = LMath.Min(minTerrainHeight, height);
                        maxTerrainHeight = LMath.Max(maxTerrainHeight, height);
                    }
                }

                float visualBottom = minTerrainHeight.ToFloat() - 0.25f;
                float visualTop = maxTerrainHeight.ToFloat() + area.height;
                float visualHeight = visualTop - visualBottom;
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"NavMesh Hole {i + 1} (visual obstacle)";
                obstacle.transform.SetParent(generatedRoot, false);
                obstacle.transform.position = new Vector3(
                    (area.minX + area.maxX) * 0.5f,
                    visualBottom + visualHeight * 0.5f,
                    (area.minZ + area.maxZ) * 0.5f);
                obstacle.transform.localScale = new Vector3(
                    area.maxX - area.minX - inset * 2f,
                    visualHeight,
                    area.maxZ - area.minZ - inset * 2f);
                DestroyUnityCollider(obstacle);

                Renderer renderer = obstacle.GetComponent<Renderer>();
                if (renderer != null && obstacleMaterial != null)
                    renderer.sharedMaterial = obstacleMaterial;
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || navData == null) return;

            if (drawNavMesh)
            {
                Gizmos.color = new Color(0.1f, 0.86f, 0.92f, 0.38f);
                for (int i = 0; i < navData.triangles.Count; i++)
                {
                    Triangle triangle = navData.triangles[i];
                    DrawRaisedLine(triangle.point1, triangle.point2, 0.03f);
                    DrawRaisedLine(triangle.point2, triangle.point3, 0.03f);
                    DrawRaisedLine(triangle.point3, triangle.point1, 0.03f);
                }
            }

            for (int i = 0; i < agentViews.Count; i++)
            {
                AgentView view = agentViews[i];
                if (view.agent == null) continue;

                if (drawPaths)
                {
                    IReadOnlyList<NavPathPoint> path = view.agent.Path;
                    Gizmos.color = AgentColors[view.colorIndex % AgentColors.Length];
                    LVector3 previous = view.agent.Position;
                    for (int pathIndex = view.agent.PathIndex; pathIndex < path.Count; pathIndex++)
                    {
                        DrawRaisedLine(previous, path[pathIndex].position, 0.16f);
                        previous = path[pathIndex].position;
                    }
                    Gizmos.DrawWireSphere(view.agent.Destination.ToVector3() + Vector3.up * 0.16f, 0.13f);
                }

                if (drawVelocities)
                {
                    LVector2 velocity = view.agent.Velocity;
                    Gizmos.color = Color.white;
                    Vector3 origin = view.agent.Position.ToVector3() + Vector3.up * (agentRadius + 0.05f);
                    Gizmos.DrawRay(
                        origin,
                        new Vector3(velocity.x.ToFloat(), 0f, velocity.y.ToFloat()) * 0.45f);
                }
            }
        }

        private static void DrawRaisedLine(LVector3 a, LVector3 b, float height)
        {
            Gizmos.DrawLine(
                a.ToVector3() + Vector3.up * height,
                b.ToVector3() + Vector3.up * height);
        }

        private void OnDestroy()
        {
            world?.Clear();
            world = null;
            agentViews.Clear();
            routePoints.Clear();

            if (navMeshVisual != null) Destroy(navMeshVisual);
            if (surfaceMaterial != null) Destroy(surfaceMaterial);
            if (agentMaterial != null) Destroy(agentMaterial);
            if (obstacleMaterial != null) Destroy(obstacleMaterial);
        }

        private void CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;

            surfaceMaterial = CreateMaterial(shader, "Nav RVO Surface", new Color(0.14f, 0.22f, 0.25f, 1f));
            agentMaterial = CreateMaterial(shader, "Nav RVO Agents", Color.white);
            obstacleMaterial = CreateMaterial(shader, "Nav RVO Obstacles", new Color(0.34f, 0.36f, 0.4f, 1f));
        }

        private static Material CreateMaterial(Shader shader, string materialName, Color color)
        {
            var material = new Material(shader) { name = materialName };
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
            return material;
        }

        private static void DestroyUnityCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private static void CreateCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(25f, 29f, -34f);
            camera.transform.LookAt(new Vector3(0f, 0f, 0f));
            camera.fieldOfView = 48f;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.085f, 1f);
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        }
    }
}
