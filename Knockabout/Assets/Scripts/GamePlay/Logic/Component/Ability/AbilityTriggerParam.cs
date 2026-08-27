using System.Collections.Generic;
using static GamePlay.AbilityEffect;
namespace GamePlay
{
    public struct AbilityTriggerParam
    {
        public TriggerType triggerType;
        public List<long> hited;

        public long sender;

        public int ability_id;

        internal static AbilityTriggerParam ADD(long target, int ability_id)
        {
            return new AbilityTriggerParam()
            {
                triggerType = TriggerType.Add,
                sender = target,
                ability_id = ability_id
            };
        }

        internal static AbilityTriggerParam Remove(long target, int ability_id)
        {
            return new AbilityTriggerParam()
            {
                triggerType = TriggerType.Remove,
                sender = target,
                ability_id = ability_id
            };
        }

        internal static AbilityTriggerParam Update(long target)
        {
            return new AbilityTriggerParam()
            {
                triggerType = TriggerType.Update,
                sender = target,
            };
        }
    }
}


