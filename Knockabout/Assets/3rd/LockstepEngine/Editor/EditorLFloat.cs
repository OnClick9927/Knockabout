// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using UnityEngine;
using UnityEditor;
namespace Lockstep
{
    /// <summary>
    /// LFloat 的 Inspector 绘制器。序列化层保存原始整数，界面显示普通小数。
    /// </summary>
    [CustomPropertyDrawer(typeof(LFloat))]
    class EditorLFloat : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var xProperty = property.FindPropertyRelative("_val");
            float LabelWidth = EditorGUIUtility.labelWidth - EditorLVectorDrawTool.LableWidthOffset;
            var labelRect = new Rect(position.x, position.y, LabelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);
            float filedWid = (position.width - LabelWidth);
            float initX = position.x + LabelWidth;
            var valRect = new Rect(initX, position.y, filedWid, position.height);
            var fVal = EditorGUI.FloatField(valRect, xProperty.intValue * 1.0f / LFloat.Precision);
            xProperty.intValue = (int)(fVal * LFloat.Precision);
        }
    }
    /// <summary>并排绘制 LVector2 的 x、y 原始定点分量。</summary>
    [CustomPropertyDrawer(typeof(LVector2))]
    public class EditorLVector2 : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var xProperty = property.FindPropertyRelative("_x");
            var yProperty = property.FindPropertyRelative("_y");
            float LabelWidth = EditorGUIUtility.labelWidth - EditorLVectorDrawTool.LableWidthOffset;
            float lableWid = EditorLVectorDrawTool.LableWid;

            var labelRect = new Rect(position.x, position.y, LabelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);
            float filedWid = (position.width - LabelWidth) / 2 - lableWid;
            float initX = position.x + LabelWidth;
            float offset = 0;
            EditorLVectorDrawTool.DrawField(position, initX, ref offset, lableWid, filedWid, xProperty, new GUIContent("x:"));
            EditorLVectorDrawTool.DrawField(position, initX, ref offset, lableWid, filedWid, yProperty, new GUIContent("y:"));
        }
    }
    /// <summary>并排绘制 LVector3 的 x、y、z 原始定点分量。</summary>
    [CustomPropertyDrawer(typeof(LVector3))]
    public class EditorLVector3 : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var xProperty = property.FindPropertyRelative("_x");
            var yProperty = property.FindPropertyRelative("_y");
            var zProperty = property.FindPropertyRelative("_z");
            float LabelWidth = EditorGUIUtility.labelWidth - EditorLVectorDrawTool.LableWidthOffset;
            float lableWid = EditorLVectorDrawTool.LableWid;

            var labelRect = new Rect(position.x, position.y, LabelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);
            float filedWid = (position.width - LabelWidth) / 3 - lableWid;
            float initX = position.x + LabelWidth;
            float offset = 0;
            EditorLVectorDrawTool.DrawField(position, initX, ref offset, lableWid, filedWid, xProperty, new GUIContent("x:"));
            EditorLVectorDrawTool.DrawField(position, initX, ref offset, lableWid, filedWid, yProperty, new GUIContent("y:"));
            EditorLVectorDrawTool.DrawField(position, initX, ref offset, lableWid, filedWid, zProperty, new GUIContent("z:"));
        }
    }
}
