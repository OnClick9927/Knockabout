using ActionEditor.Nodes;
using GamePlay;
using UnityEditor;
using UnityEngine;

class SkillModifyView : ActionEditor.Nodes.GraphNode<SkillModify>
{

    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        base.GeneratePorts(data.GetType());


    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var asset = App.asset as SkillAsset;
        var names = asset.modifyNames;
        var index = asset.modifyNames.IndexOf(data.rgName);
        index = Mathf.Max(0, index);
        if (index >= asset.modifyNames.Count)
            return;
        index = EditorGUILayout.Popup(index, asset.modifyNames.ToArray());
        data.rgName = asset.modifyNames[index];
    }
}
