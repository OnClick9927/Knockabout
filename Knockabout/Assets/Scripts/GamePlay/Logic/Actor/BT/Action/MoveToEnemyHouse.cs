using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Action), Name("追击House")]
    class MoveToEnemyHouse : BTAction
    {
        protected override State OnUpdate(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            var actor = board.actor;
            var enemy = Services.actor.FindOtherPlayer(actor.playerGUID);
            var trans_enemy = enemy.FindComponent<TransformComp>();
            var move = actor.FindComponent<MoveComp>();
            move.targetPos = trans_enemy.position;
            move.Move();
            return State.Success;
        }
    }
}