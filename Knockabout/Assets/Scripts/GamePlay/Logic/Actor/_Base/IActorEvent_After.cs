namespace GamePlay
{
    public interface IActorEvent_After : IActorEvent
    {
        void AfterExecute(Actor actor);

    }
}