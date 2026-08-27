using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("比较数值"), Node(SkillNodeGroupDefine.Condition), Attachable(typeof(SkillAsset))]
    public class SkillCompareValueSignal : SkillCondition
    {
        public enum CompareType
        {
            NotEqual, Less, LessOrEqual, Equal, Large, LargeOrEqual
        }
        public int index;
        public CompareType compareType;
        public int value;
        public bool boolValue { get { return value != 0; } set { this.value = value ? 1 : 0; } }

        public override bool Execute(SkillSignalQueue eve)
        {

            var _value = eve.GetDynamicValue(index);
            switch (compareType)
            {
                case CompareType.Less:
                    return _value < value;
                case CompareType.LessOrEqual:
                    return _value <= value;
                case CompareType.Equal:
                    return _value == value;
                case CompareType.Large:
                    return _value > value;
                case CompareType.LargeOrEqual:
                    return _value >= value;
                case CompareType.NotEqual:
                    return _value != value;

            }
            return true;
        }
    }
}


