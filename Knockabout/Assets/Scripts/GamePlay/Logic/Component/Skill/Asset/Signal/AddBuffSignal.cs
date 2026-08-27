using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("加Buff"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class AddBuffSignal : SkillSignal
    {
        public TargetType target;
        public int buff_id;

        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.AddBuff(target, this.buff_id, eve.sender, eve.Hited);

        }
    }
}


