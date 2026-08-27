using Luban;

namespace GamePlay
{
    public struct CreateActorParam
    {
        public ActorType type;
        public string player;


        public PlayerData playerInfo;



        public PlayerData.RoleInfo roleInfo;

        public static CreateActorParam Role(PlayerData player, int role_id, int lev = 0)
        {
            var role = player.FindRole(role_id);
            if (role == null)
            {
                var roleData = Configs.GetRole(role_id);
                if (lev == 0)
                    lev = roleData.DefaultLev;
                role = new PlayerData.RoleInfo()
                {
                    id = role_id,
                    level = lev,
                };
            }
            return new CreateActorParam()
            {
                type = ActorType.Role,
                player = player.guid,
                roleInfo = role
            };

        }
        public static CreateActorParam Player(PlayerData item)
        {
            return new CreateActorParam { type = ActorType.Player, player = item.guid, playerInfo = item };
        }

    }
}


