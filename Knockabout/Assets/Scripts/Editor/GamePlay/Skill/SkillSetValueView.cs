using ActionEditor.Nodes;
using GamePlay;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

class SkillSetValueView : ActionEditor.Nodes.GraphNode<SkillSetValueSignal>
{
    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        GeneratePort(Direction.Input, typeof(SkillSetValueSignal), Port.Capacity.Single, nameof(SkillSetValueSignal.In));
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
        if (type == SkillAsset.DynamicField.FieldType.Int)
            data.value = EditorGUILayout.IntField("value", data.value);
        else
            data.boolValue = EditorGUILayout.Toggle("value", data.boolValue);
    }
}


