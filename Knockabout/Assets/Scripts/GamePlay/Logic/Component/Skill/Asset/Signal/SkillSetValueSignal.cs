using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("设置数值"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class SkillSetValueSignal : SkillSignal
    {
        //[System.NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        //public new SkillSignal In;
        public int index;
        public int value;
        public bool boolValue { get { return value != 0; } set { this.value = value ? 1 : 0; } }
        public override void Execute(SkillSignalQueue eve)
        {
            eve.SetDynamicValue(index, value);

        }
    }
}


