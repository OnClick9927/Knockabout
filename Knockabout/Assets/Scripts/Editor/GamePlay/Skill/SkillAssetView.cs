using ActionAttribute;
using ActionEditor;
using ActionEditor.Nodes;
using GamePlay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using App = ActionEditor.Nodes.App;


class SkillAssetView : NodeGraphView<SkillAsset>
{
    public override bool IsFileFitAsset(string path)
    {
        return path.StartsWith("Assets/Project_GP/Skill");
    }
    public override void OnHeaderGUI()
    {
        base.OnHeaderGUI();
        GUILayout.Space(10);
        if (GUILayout.Button("Buff",EditorStyles.toolbarButton))
            BuffAssetWindow.Open();
        if (GUILayout.Button("Ability",EditorStyles.toolbarButton))
            AbilityAssetWindow.Open();
    }
    public override void OnSelectNode(GraphNode obj)
    {

    }

    protected override void AfterCreateNode(GraphElement element)
    {
        if (port == null) return;
        try
        {
            if (port.direction == Direction.Input)
                App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Output));
            else
                App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Input));

        }
        catch (Exception)
        {
        }
    }
    GraphPort port;
    protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
    {
        src.RemoveAll(x => !EditorEX.CanAttachTo(x, typeof(SkillAsset)));
        //if (element is GraphPort port)
        //{
        //    this.port = port;
        //    src.RemoveAll(x => x == typeof(BTRootView) || x == typeof(GraphGroup));
        //    //src.RemoveAll(x => port.node.GetType() != x);
        //}
        return src;
    }

    protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
    {
        var _out = start.direction == Direction.Output ? start : end;
        var _in = start.direction == Direction.Input ? start : end;

        if (start.portType == end.portType)
            return true;
        var sub = _in.portType.IsSubclassOf(_out.portType);

        return sub;
    }
}
