using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("队列"), Node(SkillNodeGroupDefine.Seq), Attachable(typeof(SkillAsset))]
    public class SkillClipSequence : SkillClip
    {
        [NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        public SkillClipSequence In;


        [NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillCondition))]
        public List<SkillCondition> conditions;


        [NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillClip))]
        public List<SkillClip> clips;



  
    }
}


