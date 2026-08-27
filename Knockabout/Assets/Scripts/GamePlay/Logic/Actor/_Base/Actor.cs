using Lockstep;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    [Backup]
    public abstract partial class Actor
    {
        [Backup] public long uid { get; set; }
        [Backup] public ActorType type { get; set; }
        [Backup] public string playerGUID { get; set; }
        [Backup] public ActorTagComp tags { get; private set; }
        [Backup(CustomCreateElement = true)]
        private List<Component> components = new List<Component>();
        private Dictionary<Type, Component> map = new Dictionary<Type, Component>();
        public PlayerActor player { get; private set; }
        public abstract PropertyComp Property { get; }
        public bool IsLocalPlayer => playerGUID == GameContext.localPlayer;
        internal bool IsBackup { get; private set; }


        public void Awake(bool backup, CreateActorParam param)
        {
            IsBackup = backup;
            if (this is PlayerActor _player)
                player = _player;
            else if (!backup)
                player = Services.actor.FindPlayer(playerGUID);
            else
                player = null;
            GameHelper.SetListToPool(components);
            map.Clear();
            if (!backup)
                OnSetParam(param);
            this.tags = CreateComponent<ActorTagComp>();
            OnAwake();
            if (!backup)
                this.InitProperty();
        }
        public void Start()
        {
            OnStart();
            for (int i = 0; i < components.Count; i++)
            {
                Component comp = components[i];
                comp.Start();
            }
            Services.view?.FindOrCreateActorView(this);
        }

        public void ExecuteEvent(IActorEvent eve)
        {

            OnEvent(eve);
            if (eve is IActorEvent_ForComp _for)
            {
                var comp = FindComponent(_for.comp);
                comp?.ExecuteEvent(eve);
            }
            else
                for (int i = 0; i < components.Count; i++)
                {
                    var t = components[i];
                    t.ExecuteEvent(eve);
                }
        }








        public T CreateComponent<T>() where T : Component, new()
        {
            T t = StaticPool.Get<T>();
            t.SetActor(this);
            components.Add(t);
            t.Awake();
            var type = typeof(T);
            map[type] = t;
            return t;
        }
        public Component FindComponent(Type type)
        {
            if (map.TryGetValue(type, out var result))
            {
                return result;
            }
            return null;
        }
        public T FindComponent<T>() where T : Component => FindComponent(typeof(T)) as T;
        public void BeginWriteBackUp()
        {
            for (int i = 0; i < components.Count; i++)
                components[i].BeginWriteBackUp();
        }
        public void EndReadBackUp()
        {
            if (this is PlayerActor _player)
                player = _player;
            else
                player = Services.actor.FindPlayer(playerGUID);
            for (int i = 0; i < components.Count; i++)
                components[i].SetActor(this);
            OnEndReadBackUp();
            for (int i = 0; i < components.Count; i++)
            {
                var t = components[i];
                t.EndReadBackUp();
            }
        }


        internal void Update()
        {
            if (tags.ContainsTag(Tags.Dead)) return;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is IUpdate update)
                    update.Update();
            }
        }
        protected abstract void OnSetParam(CreateActorParam param);
        protected abstract void OnAwake();
        protected abstract void InitProperty();
        protected virtual void OnEndReadBackUp() { }
        protected virtual void OnStart() { }

        protected virtual void OnEvent(IActorEvent eve) { }

    }
}
