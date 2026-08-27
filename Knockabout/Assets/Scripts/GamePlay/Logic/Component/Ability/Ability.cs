using ActionEditor;
using ActionAttribute;
using System.Collections.Generic;
namespace GamePlay
{
    public class Ability
    {
        public enum AbilityType
        {
            None, Update
        }
        [ReadOnly] public int Id;
        public string Name;
#if UNITY_5_3_OR_NEWER
        [Name("ÐèÒªTag")]
#endif
        [TagSelector]
        public List<string> NeedTags = new List<string>();
#if UNITY_5_3_OR_NEWER
        [Name("½ûÖ¹Tag")]
#endif

        [TagSelector]
        public List<string> NoTags = new List<string>();


        public AbilityType Type;
        [Condition(ConditionMode.Show, nameof(Type), AbilityType.Update)]
        public float cd;

        public List<AbilityEffect> effects = new List<AbilityEffect>();

        public virtual void OnAdd(AbilityComp abilityContext)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                effect.OnAdd(abilityContext);
            }

            TriggerEffect(abilityContext, AbilityTriggerParam.ADD(abilityContext.target, Id));

        }
        public virtual void OnRemove(AbilityComp abilityContext)
        {
            TriggerEffect(abilityContext, AbilityTriggerParam.Remove(abilityContext.target, Id));
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                effect.OnRemove(abilityContext);
            }

        }

        public void TriggerEffect(AbilityComp context, AbilityTriggerParam trigger)
        {
            var tags = context.actor.tags;
            if (tags.ContainsAnyTag(this.NoTags))
                return;
            if (!tags.ContainsAllTag(this.NeedTags))
                return;
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (!effect.NeedTrigger(this, context, trigger)) continue;
                effect.TriggerEffect(this, context, trigger);
            }
        }
    }

}




