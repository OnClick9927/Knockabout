using System.Collections.Generic;

namespace GamePlay
{
    public interface IActorService : IService, IBackup
    {
        Actor CreateActor(CreateActorParam param);
        T CreateActor<T>(CreateActorParam param) where T : Actor
        {
            return CreateActor(param) as T;
        }

        Actor Find(long id);
        List<Actor> FindActorsByPlayer(string player);
        T Find<T>(long id) where T : Actor, new() => Find(id) as T;
        bool Remove(long id);

        IReadOnlyList<Actor> GetActors();
        Dictionary<string, PlayerActor> GetPlayers();
        public PlayerActor FindPlayer(long id)
        {
            var actor = Find(id);
            if (actor == null) return null;
            return FindPlayer(actor.playerGUID);
        }

        PlayerActor FindPlayer(string player);
        PlayerActor FindOtherPlayer(string player);

        void EndReadBackUp();

        public static void AddModify(string player, ActorModifyAsset.Modify modify)
        {
            var _player = Services.actor.FindPlayer(player);
            var context = _player.modify;
            context.AddModify(player, modify);
        }
        public static void TryModify(Actor actor, CreateActorParam param)
        {
            var _player = actor.player;
            var context = _player.modify;
            context.TryModify(actor, param);
        }
    }
}


