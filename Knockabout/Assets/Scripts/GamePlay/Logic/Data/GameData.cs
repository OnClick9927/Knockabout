using System.Collections.Generic;
using Luban;
namespace GamePlay
{
    [Backup]
    public partial class GameData
    {
        [Backup] public GameType GameType { get; set; }
        [Backup] public string localPlayer { get; set; }
        [Backup] public long randomSeed { get; set; }
        [Backup] public List<PlayerData> players = new();
        [Backup] public int levelId { get; set; }
        public LevelData level { get; private set; }

        private Dictionary<string, PlayerData> player_map = new();
        public PlayerData FindPlayer(string guid) => player_map.TryGetValue(guid, out var player) ? player : null;

        public void Prepare()
        {
            level = Configs.GetLev(this.levelId);
            
            player_map.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                this.player_map.Add(player.guid, player);
                player.Prepare();
            }
        }


    }
}





