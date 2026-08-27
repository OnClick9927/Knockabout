using IFramework;


public class UserModel : ModelBase
{
    [Inject] PrefContext<PrefBeforeLogin> prefBeforeLogin;
    protected override void Init()
    {
        base.Init();



    }
    public void ClearInGameState()
    {
        uid = string.Empty;
    }
    public string uid { get; private set; }
    public bool IsInGame => !string.IsNullOrEmpty(uid);
    public string name { get; private set; }
    public string account => prefBeforeLogin.Value._save.account;
    public string psd => prefBeforeLogin.Value._save.psd;
    internal void SaveLoginSucceed(string account, string password, string uid, string name)
    {
        prefBeforeLogin.Value._save.account = account;
        prefBeforeLogin.Value._save.psd = password;
        this.uid = uid;
        this.name = name;
    }
}
