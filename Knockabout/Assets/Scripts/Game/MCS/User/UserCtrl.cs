using AOT;
using IFramework;
using IFramework.UI;
public class UserCtrl : CtrlBase
{
    [Inject] UserModel model;
    //[Inject] NetSession session;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    [Inject] IPrefService pref;
    [Inject] IGameStateService stateService;



    public async AsyncTask<bool> Login(string account, string psd)
    {
        bool LoginEnd(string uid, string name)
        {
            model.SaveLoginSucceed(account, psd, uid, name);
            var context = pref.FindContext<PrefBeforeLogin>();
            pref.Save(context);
            pref.Load<PrefAfterLogin>(uid);
            UI.AcceptRayCast();
            stateService.SwitchState<GameState_Main>();
            return true;
        }

        return LoginEnd(account, "xxx");

    }


}
