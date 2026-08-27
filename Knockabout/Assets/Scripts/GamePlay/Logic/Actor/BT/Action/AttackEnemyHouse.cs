using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Action), Name("攻击House")]
    class AttackEnemyHouse : BTAction
    {
        protected override State OnUpdate(Blackboard blackboard)
        {
            throw new System.NotImplementedException();
        }
    }
}