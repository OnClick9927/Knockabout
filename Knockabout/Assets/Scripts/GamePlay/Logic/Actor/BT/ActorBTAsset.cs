using ActionBuffer;
using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes.BT;

namespace GamePlay
{
    [Name("角色行为树")]
    public class ActorBTAsset : BTTree
    {
        [Buffer] private ActorBTBlackBoard _blackBoard = new ActorBTBlackBoard();
        public override Blackboard blackboard => _blackBoard;
    }
}
