using ActionEditor;
using ActionAttribute;

namespace GamePlay
{
    [Name("释放技能")]
    public class AbilityEffect_Skill : AbilityEffect
    {
        public int SkillId;
        //public TargetType Target;

        protected override void OnTriggerEffect(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            var hited = this.trigger == TriggerType.OnHit ? null : trigger.hited;
            GameHelper.DoActorEvent(context.actor, new PlaySkillEvent(this.SkillId, hited));
        }
    }

}




