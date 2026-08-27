using IFramework;
using IFramework.UI;

public class GameState_Main : IGameState
{
    [Inject(UIServiceEx.defaultName)] UIService UI;

    void IGameState.Init()
    {

    }
    async void IGameState.OnEnter(IGameState exit)
    {
        if (exit is GameState_Login)
        {
            await GameTools.LoadScene(ResDefine.mainScene);
            UI.ClearUI();
            await UI.Show(PanelNames.Main);
        }
    }

    void IGameState.OnExit(IGameState enter)
    {

    }

    void IGameState.Update()
    {

    }
    void IGameState.Quit()
    {
    }
}
