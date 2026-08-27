namespace GamePlay
{
    public struct OnUseCardEvent : IActorEvent_JustView
    {
        public int card_id;
        public int card_index;

        public OnUseCardEvent(int card_id, int card_index)
        {
            this.card_id = card_id;
            this.card_index = card_index;
        }
    }
}


