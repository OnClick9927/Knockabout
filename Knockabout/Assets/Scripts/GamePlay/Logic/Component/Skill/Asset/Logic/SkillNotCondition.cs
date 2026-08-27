using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("非"), Node(SkillNodeGroupDefine.Logic), Attachable(typeof(SkillAsset))]
    public class SkillNotCondition : SkillCondition
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output)]
        public SkillCondition Condition;
        public override bool Execute(SkillSignalQueue eve)
        {
            if (Condition == null) return true;
            if (Condition.Execute(eve))
                return true;
            return false;
        }
    }
}


