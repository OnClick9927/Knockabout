using ActionBuffer;
using Lockstep;
using System.Text;
using static GamePlay.Services;

namespace GamePlay
{

    public class GameContext
    {

        public const float logicDeltaTime = 1 / 16f;
        public const int backUpFrameCount = 32;
        public const int MaxPredictionFrameCount = 2;


        public static GameState state { get; private set; }
        public static GameData gameData { get; set; }

        public static void SetGameData(GameData game)
        {
            Services.Clear();
            gameData = game;
            game.Prepare();
        }
        public static GameType GameType => gameData.GameType;
        public static string localPlayer => gameData.localPlayer;
        public int Level => gameData.levelId;
        public static PlayerData FindPlayer(string guid) => gameData.FindPlayer(guid);
        internal static void InitGame()
        {
            state = new GameState();
            state.UnPause();
        }
        internal static void StartGame() => state.StartGame();


        internal static void WriteBackup(BufferWriter writer)
        {
            gameData.WriteBackup(writer);
            state.WriteBackup(writer);
            actor.WriteBackup(writer);
        }

        internal static void ReadBackup(BufferReader reader)
        {
            gameData.ReadBackup(reader);
            state.ReadBackup(reader);
            gameData.Prepare();
            actor.ReadBackup(reader);
        }
        internal static StringBuilder DumpString(StringBuilder builder, string perfix)
        {
            gameData.DumpString(builder, perfix);
            state.DumpString(builder, perfix);

            builder.AppendLine($"{perfix}{nameof(actor)}:[");
            builder.AppendLine($"{perfix}{{");
            actor.DumpString(builder, "\t" + perfix);
            builder.AppendLine($"{perfix}}}");
            builder.AppendLine($"{perfix}]");
            return builder;
        }
        public static int GetHash(ref int idx)
        {
            var hash = 1;

            hash += gameData.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);
            hash += state.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);
            hash += actor.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);
            return hash;
        }
    }
}


