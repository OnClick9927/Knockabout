using ActionEditor;
using ActionAttribute;
namespace GamePlay
{
    [Name("移除Tag")]
    public class BuffRemoveTag : BuffEffect
    {
       [TagSelector] public string tag;

        public override void DoEffect(Buff buff)
        {
            throw new System.NotImplementedException();
        }
    }
}


