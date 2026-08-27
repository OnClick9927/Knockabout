using ActionAttribute;

namespace GamePlay
{
    [System.Serializable]
    public class PropertyCost
    {

        public ValueEffectType costType;
        [OnValueChanged(nameof(ValidCost))]
        public PropertyType costProperty = PropertyType.HP;
        void ValidCost()
        {
            if (!Property.IsFixedProperty(costProperty))
                costProperty = PropertyType.HP;
        }


        [Condition(ConditionMode.Show, nameof(costType), ValueEffectType.Percent)]
        public PropertyType calcProperty;
        [Condition(ConditionMode.Show, nameof(costType), ValueEffectType.Fixed)]
        public long value;
        [Condition(ConditionMode.Show, nameof(costType), ValueEffectType.Percent)]
#if UNITY_5_3_OR_NEWER
        [UnityEngine.Range(0, 1)]
#endif
        public float floatValue;
    }
}
