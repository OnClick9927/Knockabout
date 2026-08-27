using IFramework;
using System.Collections.Generic;
using static GamePlay.Services;
namespace GamePlay
{
    public class LocalModeServer
    {
        private int curentFrame = 0;
        private Dictionary<long, List<PlayerInput>> inputs = new();
        public LocalModeServer()
        {
            if (GameContext.GameType != GameType.Local) return;
            CallStart();
        }

        public async void Rec(PlayerInput input)
        {
            if (GameContext.GameType != GameType.Local) return;
            if (input.frame < curentFrame)
                return;
            if (!inputs.TryGetValue(input.frame, out var result))
            {
                result = new List<PlayerInput>();
                inputs.Add(input.frame, result);
            }
            result.RemoveAll(x => x.guid == input.guid);
            result.Add(input);
            curentFrame++;
            await AsyncTask.Delay(UnityEngine.Random.Range(GameContext.logicDeltaTime / 4, GameContext.logicDeltaTime / 2));
            BroadCast(result);
        }

        private async void CallStart()
        {
            await AsyncTask.Delay(2);
            Events.Publish(new EventDefine.GameStartArgs());
        }
        private void BroadCast(List<PlayerInput> inputs)
        {
            Events.Publish(new EventDefine.RecPlayerInputsArgs(inputs));
        }
    }
}


