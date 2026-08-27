using Lockstep;
using Lockstep.RVO;

namespace GamePlay
{
    public interface IRvoService:IService
    {
        void ClearAgents();
        Agent CreateAgent(Actor actor, LVector2 pos);
        void RemoveAgent(Agent agent);
    }
}