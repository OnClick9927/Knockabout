using System;

namespace GamePlay
{
    [Backup]
    public abstract partial class Component
    {
        public abstract void SetActor(Actor actor);
        public void Awake()
        {
            OnReset();
            OnAwake();
        }
        
        public void Start() => OnStart();
        //public void Dead() => OnDead();
        public void ExecuteEvent(IActorEvent eve) => OnEvent(eve);
        public void BeginWriteBackUp() => OnBeginWriteBackUp();
        public void EndReadBackUp() => OnEndReadBackUp();


        protected abstract void OnAwake();
        protected virtual void OnStart() { }
        //protected virtual void OnDead() { }
        protected virtual void OnReset() { }
        protected virtual void OnBeginWriteBackUp() { }
        protected virtual void OnEndReadBackUp() { }

        protected virtual void OnEvent(IActorEvent eve) { }

    }
    [Backup]
    public abstract partial class Component<T> : Component where T : Actor
    {
        public T actor { get; private set; }
        public long target { get; private set; }
        public string player { get; private set; }
        public sealed override void SetActor(Actor actor)
        {
            this.target = actor.uid;
            this.player = actor.playerGUID;
            this.actor = actor as T;
        }
  
    }
}
