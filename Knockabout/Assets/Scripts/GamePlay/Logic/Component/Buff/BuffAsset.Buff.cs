using System.Collections.Generic;
using ActionEditor;
using ActionAttribute;
namespace GamePlay
{
    public partial class BuffAsset
    {
        [System.Serializable]
        public class Buff
        {
            public enum RemoveType
            {
                /// <summary>
                /// 无
                /// </summary>
                None = 0,
                /// <summary>
                /// 时间
                /// </summary>
                Time = 1,
            }
            public enum TriggerType
            {
                /// <summary>
                /// 瞬时
                /// </summary>
                None = 2,
                Jump = 4,
            }
            public enum AddType
            {
                /// <summary>
                /// 单一
                /// </summary>
                Single = 0,
                /// <summary>
                /// 替换
                /// </summary>
                Replace = 1,
                /// <summary>
                /// 叠层
                /// </summary>
                Layers = 2,
                Immediately = 3,
            }
            [ReadOnly] public int Id;
            public string Name;
#if UNITY_5_3_OR_NEWER
            [Name("需要标签")]
#endif
            [TagSelector] public List<string> needTags;
#if UNITY_5_3_OR_NEWER
            [Name("禁止标签")]
#endif
            [TagSelector] public List<string> noTags;

            [OnValueChanged(nameof(OnAddTypeChange))]
            public AddType addType;
            void OnAddTypeChange()
            {
                if (addType == AddType.Immediately)
                {
                    trigger = TriggerType.None;
                    removeType = RemoveType.None;
                }
            }

            [Condition(ConditionMode.Show, nameof(addType), AddType.Layers)]
            public int MaxLayers;

            [Condition(ConditionMode.Hide, nameof(addType), AddType.Immediately)]
            public bool IsDebuff;



            [Condition(ConditionMode.Hide, nameof(addType), AddType.Immediately)]
            public TriggerType trigger;
            [Condition(ConditionMode.Show, nameof(trigger), TriggerType.Jump)]
            [Name("触发间隔(s)")] public float TriggerGap;

            [Condition(ConditionMode.Hide, nameof(addType), AddType.Immediately)]
            public RemoveType removeType;


            [Condition(ConditionMode.Show, nameof(removeType), RemoveType.Time)]
            public float Life;
            public List<BuffEffect> Effects = new List<BuffEffect>();
        }
    }
}


