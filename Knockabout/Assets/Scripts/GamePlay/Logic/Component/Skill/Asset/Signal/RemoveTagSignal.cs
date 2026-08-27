using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("减Tag"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class RemoveTagSignal : SkillSignal
    {
        public TargetType target;
        [TagSelector]public string tag;

        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.RemoveTag(target, tag, eve.sender, eve.Hited);

        }
    }
}


