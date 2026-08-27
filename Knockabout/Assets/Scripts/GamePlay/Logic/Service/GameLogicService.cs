using System.Collections.Generic;
using static GamePlay.Services;
namespace GamePlay
{
    class GameLogicService : Service, IGameLogic
    {
        //private Dictionary<string, PlayerActor> map;

        void IGameLogic.StartGame()
        {
            for (var i = 0; i < GameContext.gameData.players.Count; i++)
            {
                var item = GameContext.gameData.players[i];
                var param = CreateActorParam.Player(item);
                var player = Services.actor.CreateActor<PlayerActor>(param);
                var pos = GameContext.gameData.level.Born[i].ToLVector3();
                player.transform.SetPosition(pos, true);
            }
            for (var i = 0; i < GameContext.gameData.players.Count; i++)
            {
                var item = GameContext.gameData.players[i];

                var player = Services.actor.FindPlayer(item.guid);
                player.StartGame();
            }


        }

        void IGameLogic.ExecuteInputs(List<PlayerInput> inputs)
        {
            if (inputs == null || inputs.Count == 0) return;
            //helper.Log($"Execute Inputs Frame  {inputs[0].frame}");
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var guid = input.guid;

                var player = actor.FindPlayer(guid);
                if (player != null)
                    player.input.ExecuteInput(input);
            }
        }



        protected override void OnDispose()
        {
        }

        protected override void OnInit()
        {

        }
    }


}


