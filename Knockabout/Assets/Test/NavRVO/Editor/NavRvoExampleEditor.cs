using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LockstepExamples.NavRvoDemo.Editor
{
    /// <summary>
    /// 为 <see cref="NavRvoExample"/> 绘制中文 Inspector 标签。
    /// 字段的中文名保存在运行时脚本的 <see cref="NavInspectorLabelAttribute"/> 上，Editor 仅负责读取和显示，
    /// 因此添加新参数时无需在两个文件中重复维护名称。
    /// </summary>
    [CustomEditor(typeof(NavRvoExample))]
    [CanEditMultipleObjects]
    public sealed class NavRvoExampleEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, string> displayNames =
            new Dictionary<string, string>();

        /// <summary>
        /// 在 Inspector 启用时缓存字段名映射。反射只执行一次，不会在每次 GUI 重绘时产生重复查找和分配。
        /// </summary>
        private void OnEnable()
        {
            displayNames.Clear();
            FieldInfo[] fields = typeof(NavRvoExample).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                NavInspectorLabelAttribute label =
                    fields[i].GetCustomAttribute<NavInspectorLabelAttribute>();
                if (label != null)
                    displayNames[fields[i].Name] = label.DisplayName;
            }
        }

        /// <summary>
        /// 按序列化顺序绘制所有顶层字段。仍然通过 PropertyField 绘制，所以原字段上的 Header、Tooltip、
        /// Range 和 Min 等 Unity 特性会照常生效，并天然支持 Undo 与多对象编辑。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                bool isScriptReference = property.propertyPath == "m_Script";
                using (new EditorGUI.DisabledScope(isScriptReference))
                {
                    string displayName;
                    if (isScriptReference)
                        displayName = "脚本";
                    else if (!displayNames.TryGetValue(property.name, out displayName))
                        displayName = property.displayName;

                    // SerializedProperty.tooltip 会读取字段上的 TooltipAttribute，中文名称不会覆盖悬停说明。
                    var content = new GUIContent(displayName, property.tooltip);
                    EditorGUILayout.PropertyField(property, content, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
