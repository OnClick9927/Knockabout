namespace GamePlay
{
    public struct OnPropertyChangedEvent : IActorEvent_After
    {
        public PropertyType type;
        public long from;
        public long to;

        public OnPropertyChangedEvent(PropertyType type, long from, long to)
        {
            this.type = type;
            this.from = from;
            this.to = to;
        }

        void IActorEvent_After.AfterExecute(Actor actor)
        {
            if (type == PropertyType.HP || type == PropertyType.MaxHP)
            {
                if (to <= 0)
                {
                    var actor_id = actor.uid;
                    var comp = actor.FindComponent<ActorTagComp>();
                    if (!comp.AddTag(Tags.Dead)) return;
                    Services.actor.Remove(actor_id);
                }
            }
        }
    }
}
