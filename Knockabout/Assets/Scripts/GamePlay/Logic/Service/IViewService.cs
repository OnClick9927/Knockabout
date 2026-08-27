namespace GamePlay
{
    public interface IViewService : IService
    {
        void FindOrCreateActorView(Actor actor);
        void DestroyUseLessActorView();
        void OnActorEvent(Actor actor, IActorEvent eve);
    }
}


