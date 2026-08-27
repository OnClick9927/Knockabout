using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System.Collections.Generic;

namespace GamePlay
{
    //public class Skill
    [Name("判断"), Node(SkillNodeGroupDefine.Logic), Attachable(typeof(SkillAsset))]
    public class SkillIFSignal : SkillSignal
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output)]
        public SkillCondition Condition;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillSignal))]
        public List<SkillSignal> Success;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillSignal))]
        public List<SkillSignal> Fail;


        public override void Execute(SkillSignalQueue eve)
        {
            var succ = Condition == null || Condition.Execute(eve);
            var list = succ ? Success : Fail;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    s.Execute(eve);
                }
            }
        }
    }
}


