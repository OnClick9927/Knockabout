using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System.Collections.Generic;

namespace GamePlay
{
    [Name("循环"), Node(SkillNodeGroupDefine.Seq), Attachable(typeof(SkillAsset))]
    public class SkillForClip : SkillClip
    {
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Input)]
        public SkillClip In;
        public int Count;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillClip))]
        public List<SkillClip> clips;
        //public override void Execute(SkillSignalQueue eve)
        //{
        //    var list = signals;
        //    if (list != null)
        //    {
        //        for (int i = 0; i < Count; i++)
        //        {
        //            for (int j = 0; j < list.Count; j++)
        //            {
        //                var s = list[j];
        //                s.Execute(eve);
        //            }
        //        }
        //    }
        //}
    }
}


