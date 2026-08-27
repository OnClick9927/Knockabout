using System;
using System.Collections.Generic;
using Lockstep;
using Lockstep.Nav;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Unity 场景中的 Lockstep 导航构建、结果预览与寻路测试入口。
/// <para>
/// 本组件不再通过根节点、Layer 或标记脚本扫描场景。可行走对象、不可行走对象和跳跃链接
/// 都由使用者在 Inspector 中显式填写，保证导出内容可见、可审查，并避免场景层级变化悄悄改变结果。
/// </para>
/// </summary>
[ExecuteAlways]
public sealed class VisualizeNavMesh : MonoBehaviour
{
    /// <summary>
    /// 一条由本组件直接保存的离线导航链接。
    /// <para>
    /// 起终点默认是世界坐标；指定 <see cref="coordinateSpace"/> 后，则解释为该 Transform
    /// 的局部坐标。这样既能直接录入世界点，也能让链接跟随场景对象移动。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class Link
    {
        [Tooltip("可选的坐标空间；为空时，起点和终点直接按世界坐标处理。")]
        public Transform coordinateSpace;

        [Tooltip("链接起点。指定坐标空间时为局部坐标，否则为世界坐标。")]
        public Vector3 startPoint;

        [Tooltip("链接终点。指定坐标空间时为局部坐标，否则为世界坐标。")]
        public Vector3 endPoint;

        [Min(0f)]
        [Tooltip("附加寻路代价。0 表示只使用端点之间的几何距离。")]
        public float cost;

        [Tooltip("开启后同时生成终点到起点的反向链接。")]
        public bool bidirectional = true;

        /// <summary>把配置中的起点换算为 Unity 世界坐标。</summary>
        public Vector3 WorldStart => coordinateSpace == null
            ? startPoint
            : coordinateSpace.TransformPoint(startPoint);

        /// <summary>把配置中的终点换算为 Unity 世界坐标。</summary>
        public Vector3 WorldEnd => coordinateSpace == null
            ? endPoint
            : coordinateSpace.TransformPoint(endPoint);

        /// <summary>把 Unity 浮点配置转换为运行时使用的世界空间定点链接。</summary>
        public NavBuildLink ToBuildLink()
        {
            return new NavBuildLink(
                ToLVector3(WorldStart),
                ToLVector3(WorldEnd),
                LMath.ToLFloat(Mathf.Max(cost, 0f)),
                bidirectional);
        }
    }

    [Header("Geometry")]
    [Tooltip("明确参与导航生成的可行走对象。每个对象只读取自身组件，不递归读取子节点。")]
    public List<GameObject> walkableObjects = new List<GameObject>();

    [Tooltip("明确参与阻挡与净空计算的不可行走对象。若对象也出现在可行走列表中，以本列表为准。")]
    public List<GameObject> nonWalkableObjects = new List<GameObject>();

    [Tooltip("由本组件直接维护的离线跳跃链接，不再需要额外的独立链接组件。")]
    public List<Link> links = new List<Link>();

    [Header("Agent")]
    [Min(0.01f)] public float cellSize = 0.25f;
    [Min(0f)] public float agentRadius = 0.5f;
    [Min(0f)] public float agentHeight = 2f;
    [Min(0f)] public float maxStepHeight = 0.5f;
    [Range(0f, 89f)] public float maxSlope = 45f;
    [Min(1)] public int minRegionCells = 1;

    [Tooltip("保持细栅格采样精度，同时合并连续共面区域。")]
    public bool mergeCoplanarCells = true;

    [Tooltip("提取共面区域轮廓并执行约束德洛内三角剖分；孔洞和边界不会被跨越。")]
    public bool useConstrainedDelaunay = true;

    [Tooltip("写入 NavData 的业务代理类型编号，与 Unity NavMesh agentTypeID 无关。")]
    public int agentType;

    [Header("Path Test")]
    public Transform start;
    public Transform end;

    // 导航数据通常很大，因此交给自定义 Inspector 仅显示摘要，避免默认 Inspector 展开序列化内容。
    [HideInInspector] public NavData data;
    [HideInInspector] public List<NavPathPoint> points = new List<NavPathPoint>();

    // 报告随场景保存，让重新选中对象或重载场景后仍能在“构建结果”页查看上次统计。
    [SerializeField, HideInInspector] private NavBuildReport lastBuildReport;
    [SerializeField, HideInInspector] private bool hasBuildResult;
    [SerializeField, HideInInspector] private int lastSerializedBytes;
    [SerializeField, HideInInspector] private double lastBuildMilliseconds;

    /// <summary>上一次构建的统计信息，只读暴露给自定义 Inspector。</summary>
    public NavBuildReport LastBuildReport => lastBuildReport;

    /// <summary>是否已经执行过至少一次构建，包括输出零三角形的失败结果。</summary>
    public bool HasBuildResult => hasBuildResult;

    /// <summary>上一次构建结果经过 BuffSerializer 编码后的字节数。</summary>
    public int LastSerializedBytes => lastSerializedBytes;

    /// <summary>上一次构建和序列化的总耗时，单位为毫秒。</summary>
    public double LastBuildMilliseconds => lastBuildMilliseconds;

    /// <summary>把 Inspector 中的浮点参数转换为与 Unity 无关的定点构建参数。</summary>
    public NavBuildSettings CreateSettings()
    {
        return new NavBuildSettings
        {
            cellSize = LMath.ToLFloat(Mathf.Max(cellSize, 0.01f)),
            agentRadius = LMath.ToLFloat(Mathf.Max(agentRadius, 0f)),
            agentHeight = LMath.ToLFloat(Mathf.Max(agentHeight, 0f)),
            maxStepHeight = LMath.ToLFloat(Mathf.Max(maxStepHeight, 0f)),
            minWalkableNormalY = LMath.ToLFloat(
                Mathf.Cos(Mathf.Clamp(maxSlope, 0f, 89f) * Mathf.Deg2Rad)),
            minRegionCells = Mathf.Max(minRegionCells, 1),
            mergeCoplanarCells = mergeCoplanarCells,
            useConstrainedDelaunay = useConstrainedDelaunay,
            agentType = agentType
        };
    }

    /// <summary>
    /// 按 Inspector 中的顺序创建纯数据链接列表。
    /// null 元素属于无效配置，直接抛出带下标的异常，防止生成结果静默漏掉链接。
    /// </summary>
    public List<NavBuildLink> CreateBuildLinks()
    {
        var result = new List<NavBuildLink>(links == null ? 0 : links.Count);
        if (links == null) return result;

        for (int i = 0; i < links.Count; i++)
        {
            Link link = links[i];
            if (link == null)
                throw new InvalidOperationException($"Links 中第 {i} 项为空，请删除或补全该项。");
            result.Add(link.ToBuildLink());
        }
        return result;
    }

    /// <summary>保存本次导航数据和统计结果，供 Gizmo、寻路测试及结果页共同使用。</summary>
    public void SetBuildResult(
        NavData navData,
        NavBuildReport report,
        int serializedBytes,
        double buildMilliseconds)
    {
        data = navData;
        lastBuildReport = report;
        hasBuildResult = true;
        lastSerializedBytes = Mathf.Max(serializedBytes, 0);
        lastBuildMilliseconds = Math.Max(buildMilliseconds, 0d);
    }

    /// <summary>把 Unity 世界坐标转换为 Lockstep 使用的定点坐标。</summary>
    public static LVector3 ToLVector3(Vector3 value)
    {
        return LVector3.CreateFromRaw(
            (long)(value.x * LFloat.Precision),
            (long)(value.y * LFloat.Precision),
            (long)(value.z * LFloat.Precision));
    }

    /// <summary>
    /// 在 Scene 视图中绘制带屏幕像素宽度的寻路线段。
    /// Camera.current 只在 Gizmo 绘制阶段有效，因此方法仅供 Gizmo 回调调用。
    /// </summary>
    public static void DrawThickLine(Vector3 p1, Vector3 p2, float width)
    {
        int count = 1 + Mathf.CeilToInt(width);
        if (count == 1)
        {
            Gizmos.DrawLine(p1, p2);
            return;
        }

        Camera camera = Camera.current;
        if (camera == null)
        {
            Debug.LogError("Camera.current is null");
            return;
        }

        Vector3 screenPoint1 = camera.WorldToScreenPoint(p1);
        Vector3 screenPoint2 = camera.WorldToScreenPoint(p2);
        Vector3 direction = (screenPoint2 - screenPoint1).normalized;
        Vector3 normal = Vector3.Cross(direction, Vector3.forward);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = 0.99f * normal * width * ((float)i / (count - 1) - 0.5f);
            Vector3 worldStart = camera.ScreenToWorldPoint(screenPoint1 + offset);
            Vector3 worldEnd = camera.ScreenToWorldPoint(screenPoint2 + offset);
            Gizmos.DrawLine(worldStart, worldEnd);
        }
    }

    /// <summary>绘制上次构建的导航三角形、寻路结果和本组件直接维护的跳跃链接。</summary>
    private void OnDrawGizmos()
    {
        if (points != null && points.Count > 1)
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < points.Count - 1; i++)
            {
                DrawThickLine(
                    points[i].position.ToVector3(),
                    points[i + 1].position.ToVector3(),
                    4f);
            }
        }

        if (data != null && data.triangles != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < data.triangles.Count; i++)
            {
                Triangle triangle = data.triangles[i];
                Gizmos.DrawLine(triangle.point1.ToVector3(), triangle.point2.ToVector3());
                Gizmos.DrawLine(triangle.point2.ToVector3(), triangle.point3.ToVector3());
                Gizmos.DrawLine(triangle.point3.ToVector3(), triangle.point1.ToVector3());
            }
        }

        if (links == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < links.Count; i++)
        {
            Link link = links[i];
            if (link == null) continue;
            Vector3 worldStart = link.WorldStart;
            Vector3 worldEnd = link.WorldEnd;
            Gizmos.DrawLine(worldStart, worldEnd);
            Gizmos.DrawSphere(worldStart, 0.12f);
            Gizmos.DrawSphere(worldEnd, 0.12f);
        }
    }
}
