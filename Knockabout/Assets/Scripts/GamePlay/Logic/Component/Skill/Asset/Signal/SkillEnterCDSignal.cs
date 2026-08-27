using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("技能进入CD"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class SkillEnterCDSignal : SkillSignal
    {
        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.DoActorEvent(Services.actor.Find(eve.sender), new SkillEnterCDEvent(eve.skill_id));
        }
    }
}


