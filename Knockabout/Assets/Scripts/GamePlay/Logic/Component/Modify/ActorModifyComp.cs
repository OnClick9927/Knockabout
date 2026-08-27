using System.Collections.Generic;
namespace GamePlay
{
    [Backup]
    public partial class ActorModifyComp : Component<PlayerActor>
    {
        [Backup] private List<int> modifies = new List<int>();

        public void AddModify(string player, ActorModifyAsset.Modify modify)
        {
            var id = modify.Id;

            if (modifies.Contains(id)) return;
            modifies.Add(id);
            if (modify.actorType == ActorType.Player)
            {
                var playerActor = Services.actor.FindPlayer(player);
                PlayEffect(modify, playerActor);
            }
            else
            {
                var _actors = Services.actor.FindActorsByPlayer(player);
                if (_actors == null) return;
                for (int j = 0; j < _actors.Count; j++)
                {
                    var actor = _actors[j];
                    if (actor.type != modify.actorType) continue;
                    if (modify.actorType == ActorType.Role)
                    {
                        if (modify.role_cfg_id != (actor as RoleActor).role_cfg_id)
                            continue;
                    }
                    PlayEffect(modify, actor);
                }

            }
        }
        private void PlayEffect(ActorModifyAsset.Modify modify, Actor actor)
        {
            for (int i = 0; i < modify.Effects.Count; i++)
            {
                var effect = modify.Effects[i];
                effect.Execute(actor, this, modify);
            }
        }
        public void TryModify(Actor actor, CreateActorParam param)
        {
            for (int j = 0; j < modifies.Count; j++)
            {
                ActorModifyAsset.Modify modify = Services.helper.Load(modifies[j]);
                if (actor.type != modify.actorType) continue;
                if (modify.actorType == ActorType.Role)
                {
                    if (modify.role_cfg_id != param.roleInfo.id)
                        continue;
                }
                PlayEffect(modify, actor);
            }

        }

        protected override void OnAwake()
        {
            modifies.Clear();
        }


    }
}


