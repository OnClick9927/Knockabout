using ActionEditor;
using ActionAttribute;

namespace GamePlay
{
    [Name("加Buff")]
    public class AbilityEffect_Buff : AbilityEffect
    {
  
        public TargetType Target;
        public int BuffId;




        protected override void OnTriggerEffect(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            CheckTarget(this.Target, this);


            if (this.trigger == TriggerType.OnHit)
                GameHelper.AddBuff(Target, this.BuffId, context.target, trigger.sender);
            else
                GameHelper.AddBuff(Target, this.BuffId, trigger.sender, trigger.hited);

        }
    }
}




