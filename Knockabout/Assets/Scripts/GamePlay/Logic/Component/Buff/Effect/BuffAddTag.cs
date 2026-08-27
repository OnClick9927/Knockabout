using ActionEditor;
using ActionAttribute;
namespace GamePlay
{
    [Name("加Tag")]
    public class BuffAddTag : BuffEffect
    {
       [TagSelector] public string tag;

        public override void DoEffect(Buff buff)
        {
        }
    }
}


