using System;
using System.Diagnostics;
using ActionBuffer;
using Lockstep.Nav;
using RGBC.Navigation.UnityEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// <see cref="VisualizeNavMesh"/> 的双页签 Inspector。
/// “构建参数”只负责编辑显式输入并生成导航数据；“构建结果”只显示上次统计和寻路测试，
/// 从而避免把体积很大的 NavData 三角形列表直接展开到默认 Inspector。
/// </summary>
[CustomEditor(typeof(VisualizeNavMesh))]
public sealed class VisualizeNavMeshEditor : Editor
{
    private static readonly string[] TabNames = { "构建参数", "构建结果" };

    private SerializedProperty walkableObjects;
    private SerializedProperty nonWalkableObjects;
    private SerializedProperty links;
    private SerializedProperty cellSize;
    private SerializedProperty agentRadius;
    private SerializedProperty agentHeight;
    private SerializedProperty maxStepHeight;
    private SerializedProperty maxSlope;
    private SerializedProperty minRegionCells;
    private SerializedProperty mergeCoplanarCells;
    private SerializedProperty useConstrainedDelaunay;
    private SerializedProperty agentType;
    private SerializedProperty start;
    private SerializedProperty end;
    private int selectedTab;

    /// <summary>缓存序列化字段，避免每次绘制 Inspector 都按字符串重复查找。</summary>
    private void OnEnable()
    {
        walkableObjects = serializedObject.FindProperty("walkableObjects");
        nonWalkableObjects = serializedObject.FindProperty("nonWalkableObjects");
        links = serializedObject.FindProperty("links");
        cellSize = serializedObject.FindProperty("cellSize");
        agentRadius = serializedObject.FindProperty("agentRadius");
        agentHeight = serializedObject.FindProperty("agentHeight");
        maxStepHeight = serializedObject.FindProperty("maxStepHeight");
        maxSlope = serializedObject.FindProperty("maxSlope");
        minRegionCells = serializedObject.FindProperty("minRegionCells");
        mergeCoplanarCells = serializedObject.FindProperty("mergeCoplanarCells");
        useConstrainedDelaunay = serializedObject.FindProperty("useConstrainedDelaunay");
        agentType = serializedObject.FindProperty("agentType");
        start = serializedObject.FindProperty("start");
        end = serializedObject.FindProperty("end");
    }

    /// <summary>绘制页签，并确保参数修改通过 Unity 序列化系统支持 Undo 和多对象状态刷新。</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        selectedTab = GUILayout.Toolbar(selectedTab, TabNames);
        EditorGUILayout.Space(6f);

        VisualizeNavMesh component = (VisualizeNavMesh)target;
        if (selectedTab == 0)
            DrawBuildParameters(component);
        else
            DrawBuildResult(component);

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>绘制全部显式几何、链接和代理参数，以及唯一的导航生成入口。</summary>
    private void DrawBuildParameters(VisualizeNavMesh component)
    {
        EditorGUILayout.LabelField("几何输入", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(walkableObjects, new GUIContent("可行走对象"), true);
        EditorGUILayout.PropertyField(nonWalkableObjects, new GUIContent("不可行走对象"), true);
        EditorGUILayout.PropertyField(links, new GUIContent("跳跃链接"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("代理设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cellSize, new GUIContent("单元格尺寸"));
        EditorGUILayout.PropertyField(agentRadius, new GUIContent("代理半径"));
        EditorGUILayout.PropertyField(agentHeight, new GUIContent("代理高度"));
        EditorGUILayout.PropertyField(maxStepHeight, new GUIContent("最大步高"));
        EditorGUILayout.PropertyField(maxSlope, new GUIContent("最大坡度"));
        EditorGUILayout.PropertyField(minRegionCells, new GUIContent("最小区域单元数"));
        EditorGUILayout.PropertyField(mergeCoplanarCells, new GUIContent("合并共面区域"));
        EditorGUILayout.PropertyField(useConstrainedDelaunay, new GUIContent("约束德洛内剖分"));
        EditorGUILayout.PropertyField(agentType, new GUIContent("代理类型"));

        EditorGUILayout.Space(8f);
        if (!GUILayout.Button("生成导航网格", GUILayout.Height(28f))) return;

        // 先提交当前帧的 Inspector 修改，按钮逻辑读取到的始终是最新参数。
        serializedObject.ApplyModifiedProperties();
        BuildNavigation(component);
        serializedObject.Update();
    }

    /// <summary>显示压缩前后规模、构建耗时和链接结果，并提供基于上次数据的寻路测试。</summary>
    private void DrawBuildResult(VisualizeNavMesh component)
    {
        if (!component.HasBuildResult)
        {
            EditorGUILayout.HelpBox("尚未生成导航网格。", MessageType.Info);
        }
        else
        {
            NavBuildReport report = component.LastBuildReport;
            int rawGridTriangles = report.walkableCells * 2;
            int removedTriangles = Mathf.Max(rawGridTriangles - report.outputTriangles, 0);

            EditorGUILayout.LabelField("构建摘要", EditorStyles.boldLabel);
            DrawReadOnlyValue("输入三角形", report.inputTriangles.ToString("N0"));
            DrawReadOnlyValue("可行走输入三角形", report.walkableInputTriangles.ToString("N0"));
            DrawReadOnlyValue(
                "可行走栅格",
                $"{report.walkableCells:N0} / {report.rasterizedCells:N0}");
            DrawReadOnlyValue("合并矩形", report.mergedRectangles.ToString("N0"));
            DrawReadOnlyValue("德洛内区域", report.delaunayRegions.ToString("N0"));
            DrawReadOnlyValue("德洛内回退区域", report.delaunayFallbackRegions.ToString("N0"));
            DrawReadOnlyValue(
                "输出三角形",
                $"{rawGridTriangles:N0} -> {report.outputTriangles:N0}，减少 {removedTriangles:N0}");
            DrawReadOnlyValue(
                "链接",
                $"接受 {report.addedLinks:N0}，拒绝 {report.rejectedLinks:N0}");
            DrawReadOnlyValue("序列化大小", EditorUtility.FormatBytes(component.LastSerializedBytes));
            DrawReadOnlyValue("构建与序列化耗时", $"{component.LastBuildMilliseconds:F2} ms");
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("寻路测试", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(start, new GUIContent("起点"));
        EditorGUILayout.PropertyField(end, new GUIContent("终点"));
        DrawReadOnlyValue(
            "路径点数",
            component.points == null ? "0" : component.points.Count.ToString("N0"));

        EditorGUILayout.Space(4f);
        if (!GUILayout.Button("测试寻路", GUILayout.Height(24f))) return;

        serializedObject.ApplyModifiedProperties();
        SearchPath(component);
        serializedObject.Update();
    }

    /// <summary>用不可编辑的双列标签显示结果，避免用户误以为统计字段是下一次构建参数。</summary>
    private static void DrawReadOnlyValue(string label, string value)
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField(label, value);
    }

    /// <summary>
    /// 收集组件中已经明确列出的输入并执行构建。生成结果会经过一次 BuffSerializer 往返，
    /// 同时验证真正交给运行时的数据能否完整序列化和反序列化。
    /// </summary>
    private static void BuildNavigation(VisualizeNavMesh component)
    {
        component.points.Clear();
        Debug.Log("Lockstep NavMesh build start.");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            NavData nav = UnityNavGeometryCollector.Build(
                component.CreateSettings(),
                component.nonWalkableObjects,
                component.walkableObjects,
                component.CreateBuildLinks(),
                out NavBuildReport report);

            byte[] bytes = BuffSerializer.ToBytes(nav);
            nav = BuffSerializer.FromBytes<NavData>(bytes);
            stopwatch.Stop();

            Undo.RecordObject(component, "Build Lockstep NavMesh");
            component.SetBuildResult(nav, report, bytes.Length, stopwatch.Elapsed.TotalMilliseconds);
            EditorUtility.SetDirty(component);
            SceneView.RepaintAll();

            if (nav.triangles.Count == 0)
            {
                Debug.LogWarning("No walkable navigation triangles were generated.");
                return;
            }

            int rawGridTriangles = report.walkableCells * 2;
            int removedTriangles = Mathf.Max(rawGridTriangles - report.outputTriangles, 0);
            Debug.Log(
                $"Lockstep NavMesh build success. " +
                $"input={report.inputTriangles}, walkableInput={report.walkableInputTriangles}, " +
                $"cells={report.walkableCells}/{report.rasterizedCells}, " +
                $"rectangles={report.mergedRectangles}, " +
                $"delaunayRegions={report.delaunayRegions}, " +
                $"delaunayFallbacks={report.delaunayFallbackRegions}, " +
                $"triangles={rawGridTriangles}->{report.outputTriangles} (-{removedTriangles}), " +
                $"links={report.addedLinks}, rejectedLinks={report.rejectedLinks}, " +
                $"serializedBytes={bytes.Length}, elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F2}.");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            Debug.LogException(exception, component);
        }
    }

    /// <summary>使用当前结果执行一次路径搜索；该操作不重新生成导航网格。</summary>
    private static void SearchPath(VisualizeNavMesh component)
    {
        component.points.Clear();
        if (component.data == null || component.start == null || component.end == null)
        {
            Debug.LogWarning("测试寻路需要已有 NavData，并同时指定起点和终点。", component);
            return;
        }

        var map = new NavMap(component.data);
        map.Search(
            VisualizeNavMesh.ToLVector3(component.start.position),
            VisualizeNavMesh.ToLVector3(component.end.position),
            component.points);
        EditorUtility.SetDirty(component);
        SceneView.RepaintAll();
    }
}
