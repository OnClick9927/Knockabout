using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;

namespace GamePlay
{
    [Name("加tag"), Node(SkillNodeGroupDefine.Signal), Attachable(typeof(SkillAsset))]
    public class AddTagSignal : SkillSignal
    {
        public TargetType target;
        [TagSelector]public string tag;

        public override void Execute(SkillSignalQueue eve)
        {
            GameHelper.AddTag(target, tag, eve.sender, eve.Hited);

        }
    }
}


