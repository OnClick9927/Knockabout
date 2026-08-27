using System.Collections.Generic;
using System.Linq;

namespace GamePlay
{

    [Backup]
    public partial class PlayerData
    {

        [Backup]
        public partial class RoleInfo
        {
            [Backup] public int id;
            [Backup] public int level;
        }
        [Backup] public PlayerType playerType;
        [Backup] public string guid;
        [Backup] public List<int> cards = new();
        [Backup] public List<RoleInfo> roles = new List<RoleInfo>();
        public Luban.PlayerProperty property;
        private Dictionary<int, RoleInfo> role_map;
        public void Prepare()
        {
            role_map = role_map ?? roles.ToDictionary(x => x.id);
        }
        public RoleInfo FindRole(int id)
        {
            return role_map.TryGetValue(id, out var result) ? result : default;
        }


    }


}


