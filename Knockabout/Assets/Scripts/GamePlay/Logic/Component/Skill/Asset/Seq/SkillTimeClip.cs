using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System;

namespace GamePlay
{
    [Name("时间"), Node(SkillNodeGroupDefine.Seq), Attachable(typeof(SkillAsset))]
    public class SkillTimeClip : SkillClip
    {
        public float time;
        [NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        public SkillClip In;
    }
}


