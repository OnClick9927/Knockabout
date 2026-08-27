using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Condition), Name("House进入攻击范围？")]
    class IsEnemyHouseInAtkRadius : BTCondition
    {
        protected override bool Condition(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            var actor = board.actor;
            var enemy = Services.actor.FindOtherPlayer(actor.playerGUID);
            var trans_enemy = enemy.FindComponent<TransformComp>();
            var trans = actor.FindComponent<TransformComp>();
            long radius = actor.Property.GetProperty(PropertyType.AtkRadius);
            return radius * radius >= (trans_enemy.position - trans.position).sqrMagnitude;
        }
    }
}