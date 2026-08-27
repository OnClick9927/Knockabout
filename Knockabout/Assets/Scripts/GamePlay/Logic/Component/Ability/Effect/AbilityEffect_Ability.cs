using ActionEditor;
using ActionAttribute;

namespace GamePlay
{
    [Name("加减能力")]
    public class AbilityEffect_Ability : AbilityEffect
    {
        public bool add;
        public TargetType Target;
        public int ability;




        protected override void OnTriggerEffect(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            CheckTarget(this.Target, this);

            if (add)
            {
                if (this.trigger == TriggerType.OnHit)
                    GameHelper.AddAbility(Target, this.ability, context.target, trigger.sender);
                else
                    GameHelper.AddAbility(Target, this.ability, trigger.sender, trigger.hited);

            }
            else
            {
                if (this.trigger == TriggerType.OnHit)
                    GameHelper.RemoveAbility(Target, this.ability, context.target, trigger.sender);
                else
                    GameHelper.RemoveAbility(Target, this.ability, trigger.sender, trigger.hited);
            }


        }
    }
}




