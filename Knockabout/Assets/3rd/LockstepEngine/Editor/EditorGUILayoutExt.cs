// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using UnityEngine;
using UnityEditor;

namespace Lockstep {
    /// <summary>
    /// 在自定义 Inspector 中编辑 LFloat/LVector 的布局辅助方法。
    /// 界面使用 float 便于输入，写回时立即量化为锁步定点值。
    /// </summary>
    public static class EditorGUILayoutExt {
        public static LFloat FloatField( string label,LFloat value,params GUILayoutOption[] options){
            return EditorGUILayout.FloatField(label, value.ToFloat(),options).ToLFloat();
        }
        public static LVector2 Vector2Field( string label,LVector2 value,params GUILayoutOption[] options){
            return EditorGUILayout.Vector2Field(label, value.ToVector2(),options).ToLVector2();
        }
        public static LVector3 Vector3Field( string label,LVector3 value,params GUILayoutOption[] options){
            return EditorGUILayout.Vector3Field(label, value.ToVector3(),options).ToLVector3();
        }  
    }
    /// <summary>定点向量 PropertyDrawer 共用的分量排版工具。</summary>
    public static class EditorLVectorDrawTool
    {
        public const float LableWidthOffset = 45;
        public const float LableWid = 20;

        /// <summary>绘制一个分量标签与输入框，并推进下一分量的水平偏移。</summary>
        public static void DrawField(Rect position, float initX, ref float offset, float lableWid, float filedWid,
            SerializedProperty property, GUIContent label)
        {
            var lableRect = new Rect(initX + offset, position.y, 70, position.height);
            EditorGUI.LabelField(lableRect, label.text);
            var valRect = new Rect(initX + offset + lableWid, position.y, filedWid, position.height);
            var fVal = EditorGUI.FloatField(valRect, property.intValue * 1.0f / LFloat.Precision);
            property.intValue = (int)(fVal * LFloat.Precision);
            offset += filedWid + lableWid;
        }
    }
}
