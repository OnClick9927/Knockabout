using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Action), Name("追击敌人")]
    class MoveToEnemyRole : BTAction
    {
        protected override State OnUpdate(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            if (!board.ExistEnemy()) return State.Failure;
            var enemy = Services.actor.Find(board.enemy_uid);
            var trans_enemy = enemy.FindComponent<TransformComp>();

            var actor = board.actor;
            var move = actor.FindComponent<MoveComp>();
            move.targetPos = trans_enemy.position;
            move.Move();
            return State.Success;
        }
    }
}