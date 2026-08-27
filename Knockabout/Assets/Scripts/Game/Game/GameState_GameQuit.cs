using IFramework;
using IFramework.UI;

public class GameState_GameQuit : IGameState
{
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] NetSession session;

    void IGameState.Init()
    {
    }

    void IGameState.OnEnter(IGameState exit)
    {
        UI?.CloseAll();
        session.Disconnect();
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
