using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("且"), Node(SkillNodeGroupDefine.Logic), Attachable(typeof(SkillAsset))]
    public class SkillAndCondition : SkillCondition
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillCondition))]
        public List<SkillCondition> Conditions;
        public override bool Execute(SkillSignalQueue eve)
        {
            if (Conditions == null) return true;
            for (int i = 0; i < Conditions.Count; i++)
            {
                SkillCondition condition = Conditions[i];
                if (!condition.Execute(eve))
                    return false;
            }
            return true;
        }
    }
}


