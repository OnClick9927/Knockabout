using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
using Lockstep;
using Lockstep.Collision;
using System.Collections.Generic;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Action), Name("寻找最近敌人")]
    class FindNearestEnemy : BTAction
    {
        protected override State OnUpdate(Blackboard blackboard)
        {
            var board = blackboard as ActorBTBlackBoard;
            var actor = board.actor;
            long sight = actor.Property.GetProperty(PropertyType.Sight);
            TransformComp transform = actor.FindComponent<TransformComp>();
            var result = Services.collision.Overlap(transform.position, sight.ToLFloat(),
                    StaticPool.Get<List<CollisionResult>>(),
                  (e) =>
                  {
                      var _actor = e.userData as Actor;
                      return _actor.player != actor.player;
                  },
                   GameCollisionLayer.Role
                    );

            var count = result.Count;
            if (count > 0)
                board.enemy_uid = (result[0].agent.userData as Actor).uid;
            StaticPool.Set(result);
            return count > 0 ? State.Success : State.Failure;
        }
    }
}