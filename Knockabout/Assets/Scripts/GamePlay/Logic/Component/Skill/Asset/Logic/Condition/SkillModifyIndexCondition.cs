using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("包含肉鸽"), Node(SkillNodeGroupDefine.Condition), Attachable(typeof(SkillAsset))]
    public class SkillModifyIndexCondition : SkillCondition
    {
        [Name("肉鸽下标"),ReadOnly]public int index;
        public override bool Execute(SkillSignalQueue eve)
        {
            return eve.ContainsModify(index);
        }
    }
}


