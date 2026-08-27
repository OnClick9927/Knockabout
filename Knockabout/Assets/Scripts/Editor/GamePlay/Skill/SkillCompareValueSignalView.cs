using ActionEditor.Nodes;
using GamePlay;
using UnityEditor;
using static GamePlay.SkillCompareValueSignal;

class SkillCompareValueSignalView : GraphNode<SkillCompareValueSignal>
{
    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        base.GeneratePorts(data.GetType());
    }

    public override void OnInspectorGUI()
    {
        var skill = App.asset as SkillAsset;
        if (skill == null) return;
        var names = skill.dynamicFields;
        data.index = EditorGUILayout.Popup("property", data.index, names.ConvertAll(x => x.name).ToArray());
        var type = skill.dynamicFields[data.index].fieldType;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.EnumPopup("type", type);
        var compare = (CompareType)EditorGUILayout.EnumPopup("compare", data.compareType);
        if (compare!= data.compareType)
        {
            if (type == SkillAsset.DynamicField.FieldType.Int || 
                (type == SkillAsset.DynamicField.FieldType.Bool && (compare == CompareType.Equal || compare== CompareType.NotEqual) ))
            {
                data.compareType = compare;
            }
        }
        if (type == SkillAsset.DynamicField.FieldType.Int)
            data.value = EditorGUILayout.IntField("value", data.value);
        else
            data.boolValue = EditorGUILayout.Toggle("value", data.boolValue);

    }
}


