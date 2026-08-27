using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes.BT;
using System.Collections.Generic;

namespace GamePlay
{
    [Backup]
    public partial class ActorBTBlackBoard : Blackboard
    {
        [System.NonSerialized] public Actor actor;
        [ReadOnly] public long enemy_uid = -1;
        [Backup]
        public List<int> _RuntimeValues = new List<int>();
        public bool ExistEnemy() => enemy_uid != -1;
        public void ResetEnemy()
        {
            enemy_uid = -1;
        }
    }
}
