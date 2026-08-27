using ActionEditor;
using ActionAttribute;
namespace GamePlay
{
    [Name("技能")]
    public class ActorModifyEffect_Skill : ActorModifyEffect
    {
        public int skill_id;
        public int index;

        public override void Execute(Actor actor, ActorModifyComp context, ActorModifyAsset.Modify modify)
        {
            throw new System.NotImplementedException();
        }
    }

}


