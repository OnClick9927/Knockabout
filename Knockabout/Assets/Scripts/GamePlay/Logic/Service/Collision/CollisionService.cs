using Lockstep;
using Lockstep.Collision;
using System;
using System.Collections.Generic;
namespace GamePlay
{
    public class CollisionService : Service, ICollisionService, IUpdate
    {
        private Lockstep.Collision.CollisionTree tree;
        void IUpdate.Update()
        {
            tree.Update();
        }
        public void ClearAgents()
        {
            tree.Clear();
        }
        public CollisionAgent CreateAgent(Actor actor, LVector2 pos, LFloat radius)
        {
            var type = actor.type;
            CollisionLayer layer = default;
            if (type == ActorType.Player)
                layer = CollisionLayer.Get(GameCollisionLayer.House);
            if (type == ActorType.Role)
                layer = CollisionLayer.Get(GameCollisionLayer.Role);
            CollisionAgent agent = CircleCollision.New(pos, radius).MakeAgent(layer, actor);
            tree.Add(agent);
            return agent;
        }
        public void RemoveAgent(CollisionAgent agent)
        {
            tree.Remove(agent);
        }
        protected override void OnDispose()
        {
            ClearAgents();
        }
        public List<CollisionResult> Overlap(LVector3 pos, LFloat radius, List<CollisionResult> results, Func<CollisionAgent, bool> fit = null, params int[] layers)
        {
            var collision = CircleCollision.New(pos.ToLVector2XZ(), radius);
            tree.OverLap(collision, results, fit, layers);
            collision.Cycle();
            return results;
        }
        protected override void OnInit()
        {
            tree = new Lockstep.Collision.CollisionTree(new Lockstep.LRect(0, 0, 5, 5), CollisionType.XZ);
            Lockstep.Collision.CollisionLayer.Get(GameCollisionLayer.House);
            Lockstep.Collision.CollisionLayer.Get(GameCollisionLayer.Role);
            Lockstep.Collision.CollisionLayer.Get(GameCollisionLayer.Skill);
        }
    }
}


