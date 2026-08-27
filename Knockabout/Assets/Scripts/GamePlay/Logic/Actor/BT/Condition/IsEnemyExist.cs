using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Condition), Name("敌人存在？")]
    class IsEnemyExist : BTCondition
    {
        protected override bool Condition(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            if (!board.ExistEnemy()) return false;
            return Services.actor.Find(board.enemy_uid) != null;
        }
    }
}