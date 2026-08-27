namespace GamePlay
{
    public struct OnTagChangeEvent : IActorEvent
    {
        public string tag;
        public bool add;
        public OnTagChangeEvent(string tag, bool add)
        {
            this.tag = tag;
            this.add = add;
        }
    }
}


