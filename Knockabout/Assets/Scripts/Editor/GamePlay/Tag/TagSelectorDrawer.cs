using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WooLocalization;
namespace GamePlay
{
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    class TagSelectorDrawer : PropertyDrawer
    {
        private float lineHeight = EditorGUIUtility.singleLineHeight;
        private float spacing = EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 获取所有标签列表（包含固定标签）
            var asset = TagAsset.Get();
            if (asset == null || asset.Tags.Count == 0)
            {
                EditorGUI.HelpBox(position, "No tags defined. Create a TagAsset.", MessageType.Warning);
                return;
            }
            var tags = asset.Tags;

            // 根据属性类型分别处理
            if (property.propertyType == SerializedPropertyType.String)
            {

                var rs = IFramework.EditorTools.RectEx.VerticalSplit(position, EditorGUIUtility.labelWidth);
                // 处理单个字符串字段
                GUI.Label(rs[0]
                    , label);
                DrawStringField(rs[1], property, label, tags);
            }
            else if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                // 处理 List<string> 或 string[] （数组）
                DrawListField(position, property, label, tags);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Unsupported type (only string and List<string>)");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                return lineHeight;
            }
            else if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                // 计算列表高度：标头 + 每个元素 + 添加按钮
                int count = property.arraySize;
                float height = lineHeight + spacing; // 列表标签行
                height += count * (lineHeight + spacing); // 元素行
                height += lineHeight + spacing; // 添加按钮行
                return height;
            }
            return lineHeight;
        }

        // 绘制单个字符串字段的下拉
        private void DrawStringField(Rect position, SerializedProperty property, GUIContent label, List<string> tags)
        {
            int currentIndex = Mathf.Max(0, tags.IndexOf(property.stringValue));
            // 如果当前值不在列表中，强行设为第一个（或保持原样，但为了安全，默认第一个）
            if (currentIndex >= tags.Count)
            {
                currentIndex = 0;
                property.stringValue = tags[0];
            }

            int newIndex = AdvancedPopup(position, currentIndex, tags.ToArray(), 350, EditorStyles.popup);
            if (newIndex != currentIndex)
            {
                property.stringValue = tags[newIndex];
            }
        }

        // 绘制列表字段（每个元素都是一个下拉）
        private void DrawListField(Rect position, SerializedProperty property, GUIContent label, List<string> tags)
        {
            // 绘制列表标题（可折叠，这里简单展示）
            Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.LabelField(labelRect, label);

            // 绘制元素
            int count = property.arraySize;
            float yOffset = labelRect.y + lineHeight + spacing;

            for (int i = 0; i < count; i++)
            {
                Rect elementRect = new Rect(position.x + 20, yOffset, position.width - 20, lineHeight); // 缩进
                var elementProperty = property.GetArrayElementAtIndex(i);
                if (elementProperty.propertyType == SerializedPropertyType.String)
                {
                    int currentIndex = Mathf.Max(0, tags.IndexOf(elementProperty.stringValue));
                    if (currentIndex >= tags.Count)
                    {
                        currentIndex = 0;
                        elementProperty.stringValue = tags[0];
                    }
                    int newIndex = AdvancedPopup(elementRect, currentIndex, tags.ToArray(), 350, EditorStyles.popup);
                    if (newIndex != currentIndex)
                    {
                        elementProperty.stringValue = tags[newIndex];
                    }
                }
                yOffset += lineHeight + spacing;
            }

            // 添加按钮
            Rect addRect = new Rect(position.x + 20, yOffset, 60, lineHeight);
            if (GUI.Button(addRect, "Add"))
            {
                property.arraySize++;
                var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                if (tags.Count > 0)
                    newElement.stringValue = tags[0];
                else
                    newElement.stringValue = "";
            }

            // 删除按钮（删除最后一个元素，这里仅示例，也可以放在每个元素旁边）
            if (count > 0)
            {
                Rect delRect = new Rect(addRect.x + 70, yOffset, 60, lineHeight);
                if (GUI.Button(delRect, "Delete"))
                {
                    property.arraySize--;
                }
            }
        }

        // 模拟 AdvancedPopup（如果您的项目有此方法，请替换为真实实现）
        private int AdvancedPopup(Rect position, int currentIndex, string[] options, float maxWidth, GUIStyle style)
        {
            return LocalizationEditorHelper.AdvancedPopup(position, currentIndex, options, maxWidth, style);
            // 这里为了演示，使用 EditorGUI.Popup 替代（实际应使用您的 LocalizationEditorHelper.AdvancedPopup）
            // 如果您的项目有 AdvancedPopup，请替换为此调用。
            // 这里用标准 Popup 确保编译通过。
        }
    }
}
