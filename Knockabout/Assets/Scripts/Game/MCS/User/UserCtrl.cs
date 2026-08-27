using AOT;
using IFramework;
using IFramework.UI;
using Proto;
public class UserCtrl : CtrlBase
{
    [Inject] UserModel model;
    [Inject] NetSession session;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    [Inject] IPrefService pref;
    [Inject] IGameStateService stateService;


    public async AsyncTask<bool> SignIn(string account, string psd)
    {
        if (string.IsNullOrEmpty(account)) return false;

        var succ = await session.Connect();
        if (!succ)
            return false;
        UI.RefuseRayCast();
        var resp = await session.Send(new SignInReq()
        {
            account = account,
            password = psd,
        });
        if (resp == null) return false;
        var _succ = resp.code == SignInResp.Err.Success;
        session.Disconnect();
        UI.AcceptRayCast();
        return _succ;
    }
    public async AsyncTask<bool> Login(string account, string psd, bool relogin = false)
    {
        if (AOTDefine.G.LocalTestMode)
            return LoginEnd(new LoginResp()
            {
                code = LoginResp.Err.Success,
                name = "xxx",
                uid = account,
                serverTime = 0

            });

        if (!relogin)
        {
            var succ = await session.Connect();
            if (!succ)
                return false;
        }

        UI.RefuseRayCast();
        if (string.IsNullOrEmpty(account)) return false;
        var resp = await session.Send(new LoginReq()
        {
            account = account,
            password = psd,
        });

        bool LoginEnd(LoginResp resp)
        {
            if (resp == null) return false;
            model.SaveLoginSucceed(account, psd, resp.uid, resp.name);
            var context = pref.FindContext<PrefBeforeLogin>();
            pref.Save(context);
            pref.Load<PrefAfterLogin>(resp.uid);
            UI.AcceptRayCast();
            if (!relogin)
                stateService.SwitchState<GameState_Main>();
            return true;
        }


        return LoginEnd(resp);
    }

    public AsyncTask Relogin()
    {
        return Login(model.account, model.psd, true);
    }
}
