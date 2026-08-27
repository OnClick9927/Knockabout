using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("加能力"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class AddAbilitySignal : SkillSignal
    {
        public TargetType target;
        public int ability_id;

        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.AddAbility(target, this.ability_id, eve.sender, eve.Hited);

        }
    }
}


