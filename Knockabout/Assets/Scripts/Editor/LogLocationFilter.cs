using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
class LogLocationFilter : ScriptableObject
{
    private static LogLocationFilter s_Instance;
    const string path = "Assets/Editor/LogLocationFilter.asset";
    public static LogLocationFilter instance
    {
        get
        {
            if (s_Instance == null)
            {
                if (!File.Exists(path))
                {
                    s_Instance = LogLocationFilter.CreateInstance<LogLocationFilter>();
                    AssetDatabase.CreateAsset(s_Instance, path);
                    AssetDatabase.Refresh();
                }
                s_Instance = AssetDatabase.LoadAssetAtPath<LogLocationFilter>(path);
            }

            return s_Instance;
        }
    }
    public List<MonoScript> scripts;
    private static Type m_ConsoleWindowType = null;
    private static FieldInfo m_ActiveTextInfo;
    private static FieldInfo m_ConsoleWindowFileInfo;


    [UnityEditor.InitializeOnLoadMethod]
    static void Init()
    {
        m_ConsoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
        m_ActiveTextInfo = m_ConsoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
        m_ConsoleWindowFileInfo = m_ConsoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
    }
    [MenuItem("Tools/日志定位过滤设置")]
    static void OpenSettings()
    {
        var settings = instance;
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
    [UnityEditor.Callbacks.OnOpenAssetAttribute(-1)]
    private static bool OnOpenAsset(int instanceID, int line)
    {
        var scripts = instance.scripts;
        if (scripts == null) return false;
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if ((m_ConsoleWindowFileInfo.GetValue(null) as EditorWindow) != EditorWindow.focusedWindow) return false;
        var eve = Event.current;
        if (eve == null || eve.clickCount != 2 || eve.button != 0)
            return false;
        if (obj is MonoScript text)
        {
            for (int i = 0; i < scripts.Count; i++)
            {
                if (scripts[i] == text)
                {
                    return FindCode();
                }
            }
        }
        return false;
    }

    static bool FindCode()
    {
        var windowInstance = m_ConsoleWindowFileInfo.GetValue(null);
        var activeText = m_ActiveTextInfo.GetValue(windowInstance);
        string[] contentStrings = activeText.ToString().Split('\n');
        for (int index = 0; index < contentStrings.Length; index++)
        {
            if (contentStrings[index].Contains("at"))
            {

                if (PingAndOpen(contentStrings[index]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool PingAndOpen(string fileContext)
    {
        string regexRule = @"at ([\w\W]*):(\d+)\)";
        Match match = Regex.Match(fileContext, regexRule);
        if (match.Groups.Count > 1)
        {
            string path = match.Groups[1].Value;
            if (path.StartsWith("Library/PackageCache"))
            {
                path = path.Substring("Library/PackageCache".Length + 1);
                var start = path.IndexOf("@");
                var end = path.IndexOf("/", start);
                path = "Packages/" + path.Replace(path.Substring(start, end - start), "");
            }
            for (int i = 0; i < instance.scripts.Count; i++)
            {
                if (AssetDatabase.GetAssetPath(instance.scripts[i]) == path)
                {
                    return false;
                }
            }






            string line = match.Groups[2].Value;
            UnityEngine.Object codeObject = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if (codeObject == null)
            {
                return false;
            }
            EditorGUIUtility.PingObject(codeObject);
            AssetDatabase.OpenAsset(codeObject, int.Parse(line));
            return true;
        }
        return false;
    }
}
