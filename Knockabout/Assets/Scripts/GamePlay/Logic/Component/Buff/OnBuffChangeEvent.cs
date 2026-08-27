namespace GamePlay
{
    public struct OnBuffChangeEvent : IActorEvent
    {
        public enum Type
        {
            EndTime,
            Remove,
            Add,
            AddLayer,
            MinusLayer
        }
        public Type type;
        public Buff buff;

        public OnBuffChangeEvent(Type type, Buff buff)
        {
            this.type = type;
            this.buff = buff;
        }
    }
}


