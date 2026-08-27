using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("技能条件")]
    public abstract class SkillCondition : SkillAction
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        public SkillCondition In;
        public abstract bool Execute(SkillSignalQueue eve);
    }
}


