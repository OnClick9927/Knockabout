/*********************************************************************************
 *Author:         OnClick
 *Date:           2025-03-27
*********************************************************************************/
using IFramework;
using IFramework.UI;
using System.Threading.Tasks;
using UnityEngine;
using WooTween;
using static IFramework.UI.UnityEventHelper;
namespace RGBC.UI
{
    public class LoginView : UIView
    {
        class View
        {
            //FieldsStart
            public TMPro.TMP_InputField Account;
            public UnityEngine.UI.Button SginIn;
            public UnityEngine.UI.Button Login;

            //FieldsEnd
            public View(LoginView context)
            {
                //InitComponentsStart
                Account = context.GetComponent<TMPro.TMP_InputField>("Account@sm");
                SginIn = context.GetComponent<UnityEngine.UI.Button>("SginIn@sm");
                Login = context.GetComponent<UnityEngine.UI.Button>("Login@sm");

                //InitComponentsEnd
            }
        }
        private View view;
        [Inject]
        private UserCtrl userCtrl;
        [Inject] private UserModel userModel;
        [Inject(UIServiceEx.defaultName)] private UIService UIService;
        protected override void InitComponents()
        {
            view = new View(this);
        }
        protected override async void OnLoad()
        {
            //view.Acc.text = Models.user.account;
            //view.PSD.text = Models.user.psd;
            //view.Login_TapTap.gameObject.SetActive(AOTDefine.G.clientChannel == AOTDefine.ClientChannel.TapTap);

            //view.Login_TapTap_dev.gameObject.SetActive(AOTDefine.G.clientChannel != AOTDefine.ClientChannel.TapTap);

            //this.BindButton(view.Login_TapTap, () =>
            //{
            //	TapTapInit.Instance.Login();
            //	UmengManager.Instance.SetEvent(UmengDefine.login_enter);
            //});
            view.Account.text = userModel.account;
            
            this.BindButton(view.Login, async () =>
            {
                var succ = await userCtrl.Login(view.Account.text, view.Account.text);
                if (succ)
                {
                    GameTools.ShowTip("登录成功");
                }
            });
            this.BindButton(view.SginIn, async () =>
            {
                var succ = await userCtrl.SignIn(view.Account.text, view.Account.text);
                if (succ)
                {
                    GameTools.ShowTip("注册成功");
                }
            });

        }
        protected override void OnShow()
        {
            this.transform.DoJumpLocalPosition(transform.position, 1, Vector3.up*100).AsDisposable().AddTo(this);

        }
        protected override void OnHide() { }
        protected override void OnClose() { }
        protected override void OnBecameInvisible() { }
        protected override void OnBecameVisible() { }
    }
}
