using IFramework;
using static GamePlay.Services;
namespace GamePlay
{
    public abstract class ActorView : GameObjectView, IPoolAbleGameObjectView
    {
        public long target { get; private set; }
        public ActorType type { get; private set; }
        string IPoolAbleGameObjectView.PoolKey { get; set; }
        public void Update() => OnUpdate();
        public AsyncTask Destroy(bool immediate) => OnDestroy(immediate);
        public void Init(long target, ActorType type)
        {
            this.target = target;
            this.type = type;
            OnInit();
        }

        public void BindActor(Actor actor) => OnBindActor(actor);
        protected abstract void OnBindActor(Actor actor);
        protected abstract void OnUpdate();
        public abstract void OnDead();
        protected abstract void OnInit();
        protected abstract AsyncTask OnDestroy(bool immediate);

        public abstract void SyncTransform();
    }
    public abstract class ActorView<T> : ActorView where T : Actor
    {
        public T actor { get; private set; }
        protected sealed override void OnBindActor(Actor actor)
        {
            this.actor = actor as T;
            if (this.actor == null)
            {
                helper.Error($"Type Not Fit {actor}-{this.GetType()}");
            }
        }
    }
}


