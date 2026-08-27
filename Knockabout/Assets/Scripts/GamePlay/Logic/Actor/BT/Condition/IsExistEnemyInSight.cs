using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using ActionEditor.Nodes.BT;
using Lockstep;
using Lockstep.Collision;
using System.Collections.Generic;
namespace GamePlay
{
    [Attachable(typeof(ActorBTAsset)), Node(BTNodeTypes.Condition), Name("视野内有敌人？")]
    class IsExistEnemyInSight : BTCondition
    {
        protected override bool Condition(Blackboard blackboard)
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
            StaticPool.Set(result);
            return count > 0;
        }
    }
}