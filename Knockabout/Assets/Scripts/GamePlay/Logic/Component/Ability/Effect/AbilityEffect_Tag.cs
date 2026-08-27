using ActionEditor;
using ActionAttribute;
using System.Collections.Generic;

namespace GamePlay
{

    [Name("加减Tag")]
    public class AbilityEffect_Tag : AbilityEffect
    {
        public bool Add;
        public TargetType Target;
        [TagSelector] public List<string> tags;
        protected override void OnTriggerEffect(Ability ability, AbilityComp context,
            AbilityTriggerParam trigger)
        {
            CheckTarget(this.Target, this);


            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];

                if (this.Add)
                {
                    if (this.trigger == TriggerType.OnHit)
                        GameHelper.AddTag(Target, tag, context.target, trigger.sender);
                    else
                        GameHelper.AddTag(Target, tag, trigger.sender, trigger.hited);
                }
                else
                {
                    if (this.trigger == TriggerType.OnHit)
                        GameHelper.RemoveTag(Target, tag, context.target, trigger.sender);
                    else
                        GameHelper.RemoveTag(Target, tag, trigger.sender, trigger.hited);
                }
            }

        }
    }
}




