using ActionEditor;
using ActionAttribute;
using System.Collections.Generic;
namespace GamePlay
{
    [Name("属性")]
    public class ActorModifyEffect_Property : ActorModifyEffect
    {
        [System.Serializable]
        public class Data
        {
            public ValueEffectType modfyType;
            [OnValueChanged(nameof(ValidCost))]
            public PropertyType property = PropertyType.HP;
            void ValidCost()
            {
                if (Property.IsFixedProperty(property))
                    property = PropertyType.MaxHP;
            }

            [Condition(ConditionMode.Show, nameof(modfyType), ValueEffectType.Fixed)]
            public long value;
            [Condition(ConditionMode.Show, nameof(modfyType), ValueEffectType.Percent)]
#if UNITY_5_3_OR_NEWER
            [UnityEngine.Range(-1.1f, 5)]
#endif
            public float floatValue;

        }
        [Name("属性修改")]

        public List<Data> datas;

        public override void Execute(Actor actor, ActorModifyComp context,
            ActorModifyAsset.Modify modify)
        {
            using (var scope = actor.Property.BeginPropChange())
                for (int i = 0; i < datas.Count; i++)
                {
                    var data = datas[i];
                    if (data.modfyType == ValueEffectType.Percent)
                        scope.PushPropPercent(PropertyLayer.ActorModify,
                            data.property,
                            data.floatValue
                            );
                    else
                        scope.PushProp(PropertyLayer.ActorModify,
                            data.property,
                            data.value
                            );
                }
        }

    }

}


