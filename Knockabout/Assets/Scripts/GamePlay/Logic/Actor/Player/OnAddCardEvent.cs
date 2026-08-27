using Lockstep;

namespace GamePlay
{

    public struct OnAddCardEvent : IActorEvent_JustView
    {
        public int card_id;
        public LVector3 pos;
        public bool Success;

        public OnAddCardEvent(int card_id, LVector3 pos, bool success)
        {
            this.card_id = card_id;
            this.pos = pos;
            Success = success;
        }
    }
}


