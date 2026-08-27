using ActionBuffer;
using Lockstep;
using System.Collections.Generic;
using System.Text;
namespace GamePlay
{
    partial class ActorService : Service, IActorService, IUpdate
    {
        private Dictionary<long, Actor> dict = new Dictionary<long, Actor>();
        private List<Actor> _actors = new List<Actor>();
        private Dictionary<string, PlayerActor> map_player = new Dictionary<string, PlayerActor>();
        private Dictionary<string, List<Actor>> Own = new Dictionary<string, List<Actor>>();
        public IReadOnlyList<Actor> GetActors() => _actors;
        public PlayerActor FindOtherPlayer(string player)
        {
            foreach (var item in map_player)
            {
                if (item.Key != player) return item.Value;
            }
            return null;
        }

        public Dictionary<string, PlayerActor> GetPlayers() => map_player;
        public List<Actor> FindActorsByPlayer(string player) => Own.TryGetValue(player, out var result) ? result : null;
        protected override void OnInit()
        {
            map_player = new();
            Own = new();
        }
        public bool Remove(long id)
        {
            var succ = dict.Remove(id, out var actor);
            if (succ)
            {
                _actors.Remove(actor);
                if (actor is PlayerActor)
                    map_player.Remove(actor.playerGUID);
                else
                {

                    var list = FindActorsByPlayer(actor.playerGUID);
                    list.Remove(actor);
                }

                StaticPool.SetByRealType(actor);

            }
            return succ;
        }


        private void TryAddPlayer(Actor actor)
        {
            if (actor is PlayerActor player)
                map_player[actor.playerGUID] = player;
            else
            {
                var _player = actor.playerGUID;
                if (!Own.TryGetValue(_player, out var result))
                {
                    result = new();
                    Own.Add(_player, result);
                }
                result.Add(actor);
            }
        }
        public Actor CreateActor(CreateActorParam param)
        {
            var actor = Create(param.type);
            actor.uid = GameContext.state.GenUid();
            actor.type = param.type;
            actor.playerGUID = param.player;
            actor.Awake(false,param);
            _actors.Add(actor);
            dict[actor.uid] = actor;
            actor.Start();
            TryAddPlayer(actor);
            IActorService.TryModify(actor, param);
            return actor;
        }
        public Actor Find(long id) => dict.TryGetValue(id, out var actor) ? actor : null;

        public PlayerActor FindPlayer(string player) => map_player.TryGetValue(player, out var p) ? p : null;
        void IUpdate.Update()
        {
            var actors = _actors;
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                actor.Update();
            }
        }

        protected override void OnDispose()
        {
            GameHelper.SetListToPool(_actors);
            dict.Clear();
            map_player.Clear();
            Own.Clear();
        }

        private Actor Create(ActorType type)
        {
            if (type == ActorType.Player)
                return StaticPool.Get<PlayerActor>();
            if (type == ActorType.Role)
                return StaticPool.Get<RoleActor>();
            return default;
        }

        public void EndReadBackUp()
        {
            foreach (var actor in _actors)
            {
                actor.EndReadBackUp();
                actor.Start();
            }
        }
        public void ReadBackup(BufferReader reader)
        {
            OnDispose();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var type = (ActorType)reader.ReadEnum(typeof(ActorType));
                var actor = Create(type);
                actor.Awake(true,default);
                actor.ReadBackup(reader);
                _actors.Add(actor);
                dict[actor.uid] = actor;
                TryAddPlayer(actor);
            }
        }
        public void WriteBackup(BufferWriter writer)
        {
            var actors = _actors;
            var count = actors.Count;
            writer.WriteInt32(count);

            for (int i = 0; i < count; i++)
            {
                var actor = actors[i];
                actor.BeginWriteBackUp();
                writer.WriteEnum(actor.type);
                actor.WriteBackup(writer);
            }

        }
        public void DumpString(StringBuilder builder, string perfix)
        {
            var actors = _actors;
            builder.AppendLine($"{perfix}{nameof(actors)}:[");
            for (int i = 0; i < actors.Count; i++)
            {
                builder.AppendLine($"{perfix}{{");
                actors[i].DumpString(builder, "\t" + perfix);
                builder.AppendLine($"{perfix}}}");
            }
            builder.AppendLine($"{perfix}]");
        }

        public int GetHash(ref int idx)
        {
            int hash = 1;
            var actors = _actors;
            for (int i = 0; i < actors.Count; i++)
                hash += actors[i].GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);
            return hash;
        }


    }
}


