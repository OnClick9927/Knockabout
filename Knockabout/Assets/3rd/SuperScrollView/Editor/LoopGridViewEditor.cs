using UnityEngine;
using UnityEditor;


namespace SuperScrollView
{

    [CustomEditor(typeof(LoopGridView))]
    public class LoopGridViewEditor : Editor
    {

        SerializedProperty mGridFixedType;
        SerializedProperty mGridFixedRowOrColumn;
        SerializedProperty mItemSnapEnable;
        SerializedProperty mArrangeType;
        SerializedProperty mItemPrefabDataList;
        SerializedProperty mItemSnapPivot;
        SerializedProperty mViewPortSnapPivot;
        SerializedProperty mPadding;
        SerializedProperty mItemSize;
        SerializedProperty mItemPadding;
        SerializedProperty mItemRecycleDistance;

        GUIContent mItemSnapEnableContent = new GUIContent("ItemSnapEnable");
        GUIContent mArrangeTypeGuiContent = new GUIContent("ArrangeType");
        GUIContent mItemPrefabListContent = new GUIContent("ItemPrefabList");
        GUIContent mItemSnapPivotContent = new GUIContent("ItemSnapPivot");
        GUIContent mViewPortSnapPivotContent = new GUIContent("ViewPortSnapPivot");
        GUIContent mGridFixedTypeContent = new GUIContent("GridFixedType");
        GUIContent mPaddingContent = new GUIContent("Padding");
        GUIContent mItemSizeContent = new GUIContent("ItemSize");
        GUIContent mItemPaddingContent = new GUIContent("ItemPadding");
        GUIContent mGridFixedRowContent = new GUIContent("RowCount");
        GUIContent mGridFixedColumnContent = new GUIContent("ColumnCount");
        GUIContent mItemRecycleDistanceContent = new GUIContent("RecycleDistance");
        private LoopGridView view => target as LoopGridView;
        protected virtual void OnEnable()
        {
            mGridFixedType = serializedObject.FindProperty("mGridFixedType");
            mItemSnapEnable = serializedObject.FindProperty("mItemSnapEnable");
            mArrangeType = serializedObject.FindProperty("mArrangeType");
            mItemPrefabDataList = serializedObject.FindProperty("mItemPrefabDataList");
            mItemSnapPivot = serializedObject.FindProperty("mItemSnapPivot");
            mViewPortSnapPivot = serializedObject.FindProperty("mViewPortSnapPivot");
            mGridFixedRowOrColumn = serializedObject.FindProperty("mFixedRowOrColumnCount");
            mItemPadding = serializedObject.FindProperty("mItemPadding");
            mPadding = serializedObject.FindProperty("mPadding");
            mItemSize = serializedObject.FindProperty("mItemSize");
            mItemRecycleDistance = serializedObject.FindProperty("mItemRecycleDistance");
        }

        private void Draw()
        {
            if (GUILayout.Button("updatePadding"))
            {
                var pad = view.Padding;
                view.ForceUpdatePadding(pad);
                view.ForceUpdateItemPadding(view.ItemPadding);

            }
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LoopGridView tListView = serializedObject.targetObject as LoopGridView;
            if (tListView == null)
            {
                return;
            }
            EditorGUILayout.PropertyField(mItemPrefabDataList, mItemPrefabListContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(mGridFixedType, mGridFixedTypeContent);
            if (mGridFixedType.enumValueIndex == (int)GridFixedType.ColumnCountFixed)
            {
                EditorGUILayout.PropertyField(mGridFixedRowOrColumn, mGridFixedColumnContent);
            }
            else
            {
                EditorGUILayout.PropertyField(mGridFixedRowOrColumn, mGridFixedRowContent);
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(mPadding, mPaddingContent, true);
            EditorGUILayout.PropertyField(mItemSize, mItemSizeContent);
            EditorGUILayout.PropertyField(mItemPadding, mItemPaddingContent);
            EditorGUILayout.PropertyField(mItemRecycleDistance, mItemRecycleDistanceContent);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(mItemSnapEnable, mItemSnapEnableContent);
            if (mItemSnapEnable.boolValue == true)
            {
                EditorGUILayout.PropertyField(mItemSnapPivot, mItemSnapPivotContent);
                EditorGUILayout.PropertyField(mViewPortSnapPivot, mViewPortSnapPivotContent);
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(mArrangeType, mArrangeTypeGuiContent);

            serializedObject.ApplyModifiedProperties();
            if (!Application.isPlaying) return;
            GUILayout.Space(30);
            Draw();
        }
    }
}
