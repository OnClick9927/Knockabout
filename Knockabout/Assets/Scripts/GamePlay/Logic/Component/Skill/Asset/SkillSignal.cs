using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{

    [Name("技能信号")]
    public abstract class SkillSignal : SkillClip
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        public SkillSignal In;
        public abstract void Execute(SkillSignalQueue eve);

    }
}


