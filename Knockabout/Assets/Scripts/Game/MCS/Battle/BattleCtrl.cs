using GamePlay;
using IFramework;
using Luban;
using System.Collections.Generic;
using UnityEngine;

public class BattleCtrl : CtrlBase
{
    [Inject] GGame game;
    [Inject] IGameStateService stateService;

    internal void EnterGame(int lev = -1)
    {
        if (lev == -1)
            lev = Configs.GetGlobal().Firstlev;
        var levData = Configs.GetLev(lev);
        var localGUID = SystemInfo.deviceUniqueIdentifier;

        var playerBaseProperty = Configs.GetPlayerProperty(Configs.GetGlobal().PlayerBaseProperty);



        PlayerData player = new PlayerData()
        {
            guid = localGUID,
            playerType = PlayerType.None,
            cards = new List<int>(),
            roles = new List<PlayerData.RoleInfo>() {
                new (){id=1,level=1,}
            },
            property = playerBaseProperty,
        };
        PlayerData robot = new PlayerData()
        {
            guid = "Robot",
            playerType = PlayerType.Robot,
            cards = new List<int>(),
            roles = new List<PlayerData.RoleInfo>() {
             new (){id=1,level=1,}
            },
            property = playerBaseProperty,
        };

        GameContext.SetGameData(new GameData()
        {
            GameType = GameType.Local,
            localPlayer = localGUID,
            players = new() { player, robot },
            levelId = lev,
            randomSeed = 7721,
        });
        stateService.SwitchState<GameState_GamePlay>();
    }
}
