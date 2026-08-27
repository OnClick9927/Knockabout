using Lockstep;
using Lockstep.Collision;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    public interface ICollisionService:IService
    {
        void ClearAgents();
        CollisionAgent CreateAgent(Actor actor, LVector2 pos, LFloat radius);
        void RemoveAgent(CollisionAgent agent);
        List<CollisionResult> Overlap(LVector3 pos, LFloat radius, List<CollisionResult> results, Func<CollisionAgent, bool> fit = null, params int[] layers);
    }
}