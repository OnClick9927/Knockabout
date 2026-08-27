using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Condition), Name("敌人进入攻击范围？")]
    class IsEnemyInAtkRadius : BTCondition
    {
        protected override bool Condition(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            if (!board.ExistEnemy()) return false;
            var enemy = Services.actor.Find(board.enemy_uid);
            var trans_enemy = enemy.FindComponent<TransformComp>();
            var actor = board.actor;
            var trans = actor.FindComponent<TransformComp>();
            long radius = actor.Property.GetProperty(PropertyType.AtkRadius);
            return radius * radius >= (trans_enemy.position - trans.position).sqrMagnitude;
        }
    }
}