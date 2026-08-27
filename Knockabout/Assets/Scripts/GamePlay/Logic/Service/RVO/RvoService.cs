using Lockstep;
using Lockstep.RVO;
namespace GamePlay
{
    public class RvoService : Service, IUpdate, IRvoService
    {
        Simulator simulator;
        protected override void OnInit()
        {
            simulator = new Simulator();
            simulator.timeStep_ = GameContext.logicDeltaTime.ToLFloat();
            simulator.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 2.0f, 2.0f, LVector2.zero);
            simulator.processObstacles();
        }
        public void Update()
        {
            this.simulator.doStep();
        }
        public Agent CreateAgent(Actor actor, LVector2 pos)
        {
            return simulator.addAgent(pos);
        }
        public void RemoveAgent(Agent agent)
        {
            simulator.delAgent(agent.id_);
        }
        public void ClearAgents()
        {
            simulator.ClearAgents();
        }
        protected override void OnDispose()
        {
            ClearAgents();
        }


    }
}


