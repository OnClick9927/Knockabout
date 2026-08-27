using Lockstep;

namespace GamePlay
{
    [Backup]
    public partial class TransformComp : Component<Actor>, IUpdate
    {
        [Backup] public LVector3 position;
        [Backup] public LVector2 dir;
        [Backup] private bool dirty;
        [Backup] public bool initPos;
        public float radius;
        Lockstep.Collision.CollisionAgent collision;
        protected override void OnReset()
        {
            base.OnReset();
            collision = null;

        }
        protected override void OnAwake()
        {
            position = LVector2.zero;
            dir = LVector2.zero;
        }
        public void SetDir(LVector2 dir)
        {
            if (this.dir == dir) return;
            this.dir = dir;
            dirty = true;
        }
        public void SetPosition(LVector3 position, bool init = false)
        {
            this.initPos = init;
            if (this.position == position) return;
            this.position = position;
            dirty = true;
        }
        protected override void OnStart()
        {
            base.OnStart();
            collision = Services.collision
           .CreateAgent(actor, position.ToLVector2XZ(), radius.ToLFloat());
        }
        protected override void OnEvent(IActorEvent eve)
        {
            base.OnEvent(eve);

            if (eve is OnTagChangeEvent tags && tags.tag == Tags.Dead && tags.add)
            {
                Services.collision.RemoveAgent(collision);
                collision = null;
            }
        }
        void IUpdate.Update()
        {
            if (!dirty) return;
            collision.SetPos(position.ToLVector2XZ());
            GameHelper.DoActorEvent(actor, new OnTransformChangeEvent());
            dirty = false;
        }
    }
}