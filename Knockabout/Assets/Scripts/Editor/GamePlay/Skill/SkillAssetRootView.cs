using ActionAttribute;
using ActionEditor.Nodes;
using GamePlay;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
class SkillModifyIndexConditionView : ActionEditor.Nodes.GraphNode<SkillModifyIndexCondition>
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
        var index = data.index;
        index = Mathf.Max(0, index);
        if (index >= asset.modifyNames.Count)
            return;
        index = EditorGUILayout.Popup(index, asset.modifyNames.ToArray());
        data.index = index;
   
    }
}

class SkillAssetRootView : ActionEditor.Nodes.GraphNode<SkillAssetRoot>
{
    Vector2 scroll;
    public override void OnInspectorGUI()
    {
        scroll = GUILayout.BeginScrollView(scroll);
        EditorEX.CreateEditor(Data).OnInspectorGUI();
        GUILayout.Label("Asset↓↓↓");
        EditorEX.CreateEditor(App.asset).OnInspectorGUI();
        GUILayout.EndScrollView();
    }
    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        var names = Enum.GetNames(typeof(SkillEventType));
        foreach (var item in names)
        {
            GeneratePort(Direction.Output, typeof(SkillClipSequence), Port.Capacity.Multi, item);
        }
        GeneratePorts(typeof(SkillAsset));
      
    }
}
