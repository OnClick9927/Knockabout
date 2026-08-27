using System;
using ActionAttribute;

namespace GamePlay
{
    [Serializable]
    public class SkillProperty
    {
        public SkillPropertyType type;
   
        public ValueEffectType effect;
        [Condition(ConditionMode.Show, nameof(effect), ValueEffectType.Fixed)]
        public long value;
        [Condition(ConditionMode.Show, nameof(effect), ValueEffectType.Percent)]
        public PropertyType clac;
        [Condition(ConditionMode.Show, nameof(effect), ValueEffectType.Percent)]
#if UNITY_5_3_OR_NEWER
        [UnityEngine.Range(-1, 10)]
#endif
        public float percent;
    }
}


