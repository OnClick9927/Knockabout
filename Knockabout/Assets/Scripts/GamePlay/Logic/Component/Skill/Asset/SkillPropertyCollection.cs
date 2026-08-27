using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("技能属性"), Node(SkillNodeGroupDefine.Skill), Attachable(typeof(SkillAsset))]
    public class SkillPropertyCollection : ActionEditor.Nodes.NodeData
    {
        [NonSerialized, NodePort(NodePortAttribute.Direction.Input)] public SkillPropertyCollection In;
        public List<SkillProperty> properties;
    }
}


