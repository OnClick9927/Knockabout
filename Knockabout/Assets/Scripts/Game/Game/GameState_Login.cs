using IFramework;
using IFramework.UI;

public class GameState_Login : IGameState
{
    void IGameState.Init()
    {

    }
    [Inject] UserModel userModel;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    [Inject] IPrefService pref;
    async void IGameState.OnEnter(IGameState exit)
    {
        await GameTools.LoadScene(ResDefine.loginScene);
        UI.ClearUI();
        userModel.ClearInGameState();
        pref.ClearAll();
        pref.Load<PrefBeforeLogin>();
        UI.Show(PanelNames.Login).Coroutine();
    }

    void IGameState.OnExit(IGameState exit)
    {

    }

    void IGameState.Update()
    {

    }

    void IGameState.Quit()
    {
    }
}
