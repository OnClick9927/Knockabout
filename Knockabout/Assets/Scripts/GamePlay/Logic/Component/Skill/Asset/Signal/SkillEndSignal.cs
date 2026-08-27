using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("技能结束"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class SkillEndSignal : SkillSignal
    {
        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.DoActorEvent(Services.actor.Find(eve.sender), new OnSkillEndEvent(eve.skill_id));
        }
    }
}


