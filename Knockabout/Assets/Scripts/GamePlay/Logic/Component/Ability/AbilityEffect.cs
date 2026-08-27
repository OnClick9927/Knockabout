namespace GamePlay
{
    public abstract class AbilityEffect
    {
        public enum TriggerType
        {
            Update,//一直跑
            Add,
            Remove,




            Born,//
            Dead,//
            Hit,//命中
            OnHit,//被命中,
        }

        [ActionAttribute.ReadOnly] public TriggerType trigger;

   
     
        public static void CheckTarget(TargetType Target, AbilityEffect effect)
        {
            var trigger_type = effect.trigger;
            if (trigger_type == TriggerType.Update ||
                trigger_type == TriggerType.Add ||
                trigger_type == TriggerType.Remove ||
                trigger_type == TriggerType.Born ||
                trigger_type == TriggerType.Dead
                )
            {
                if (Target== TargetType.Target || Target == TargetType.TargetPlayer)
                {
                    Services.helper.Error($"{trigger_type} not has target ");
                }
            }
        }



        public bool NeedTrigger(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            var triggerType = trigger.triggerType;
      
            if (triggerType != this.trigger) return false;
            //if (triggerType == AbilityTriggerType.Update ||
            //    triggerType == AbilityTriggerType.OnHit ||
            //    triggerType == AbilityTriggerType.Hit ||
            //    triggerType == AbilityTriggerType.Born ||
            //    triggerType == AbilityTriggerType.Dead)
            //{
            //}
                return OnNeedTrigger(ability, context, trigger);

            //return false;

        }
        protected virtual bool OnNeedTrigger(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            return true;
        }
        public virtual void OnAdd(AbilityComp abilityContext) { }
        public virtual void OnRemove(AbilityComp abilityContext) { }
        protected abstract void OnTriggerEffect(Ability ability, AbilityComp context, AbilityTriggerParam trigger);
        public void TriggerEffect(Ability ability, AbilityComp context, AbilityTriggerParam trigger)
        {
            OnTriggerEffect(ability,context, trigger);
        }
    }

}




