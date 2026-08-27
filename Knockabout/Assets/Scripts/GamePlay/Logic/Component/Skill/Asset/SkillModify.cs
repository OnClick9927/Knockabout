using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("肉鸽"), Node(SkillNodeGroupDefine.Skill), Attachable(typeof(SkillAsset))]
    public class SkillModify : ActionEditor.Nodes.NodeData
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Input)] public SkillModify In;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output)] public SkillModifyProperty property;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillSetValueSignal))] public List<SkillSetValueSignal> sets;
        [Name("肉鸽名字")][ReadOnly]public string rgName;

        //[System.NonSerialized] public Dictionary<SkillEventType, SkillActionSequence> modifys;

        //[System.NonSerialized] public List<SkillSequenceAction> seqences;
        //[System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillModifyData))] public List<SkillModifyData> modify;

    }

}


