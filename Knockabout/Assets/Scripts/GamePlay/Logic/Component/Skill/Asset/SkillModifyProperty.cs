using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("肉鸽属性"),Node(SkillNodeGroupDefine.Skill), Attachable(typeof(SkillAsset))]
    public class SkillModifyProperty:ActionEditor.Nodes.NodeData
    {
        [NonSerialized, NodePort(NodePortAttribute.Direction.Input)] public SkillModifyProperty In;

        public List<SkillProperty> properties;

   


    }
}


