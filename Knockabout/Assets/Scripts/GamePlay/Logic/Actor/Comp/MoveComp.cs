using Lockstep;
using Lockstep.RVO;

namespace GamePlay
{
    public partial class MoveComp : Component<Actor>
    {
        Lockstep.RVO.Agent agent;
        [Backup] public LVector3 targetPos;
        [Backup] private bool stop;
        TransformComp transform;
        protected override void OnReset()
        {
            base.OnReset();
            agent = null;

        }
        protected override void OnAwake()
        {
        }
        protected override void OnStart()
        {
            base.OnStart();
            transform = actor.FindComponent<TransformComp>();
            agent = Services.rvo
           .CreateAgent(actor, transform.position.ToLVector2XZ());
            agent.maxSpeed_ = actor.Property.GetProperty(PropertyType.Speed).value.ToLFloat();
        }
        protected override void OnEvent(IActorEvent eve)
        {
            base.OnEvent(eve);
            if (eve is OnPropertyChangedEvent prop && prop.type == PropertyType.Speed)
            {
                agent.maxSpeed_ = prop.to.ToLFloat();
            }
            if (eve is OnTagChangeEvent tags && tags.tag == Tags.Dead && tags.add)
            {
                Services.rvo.RemoveAgent(agent);
                agent = null;

            }
        }

        public void StopMove(bool stop = true)
        {
            this.stop = stop;
        }
        public void Move()
        {
            if (agent == null) return;
            LVector2 pos = agent.position_;
            LVector2 vel = agent.prefVelocity_;
            transform.dir = vel;
            transform.SetPosition(pos.ToLVector3XZ());
            LVector3 goalVector = LVector3.zero;
            if (!stop)
            {
                goalVector = targetPos - transform.position;
                if (RVOMath.absSq(goalVector) > LFloat.one)
                    goalVector = RVOMath.normalize(goalVector);
            }
            agent.prefVelocity_ = goalVector.ToLVector2XZ();
            transform.SetDir(agent.prefVelocity_);
        }
    }
}