/*********************************************************************************
 *Author:         OnClick
 *Date:           2025-12-28
*********************************************************************************/
using IFramework;
using IFramework.UI;
using static IFramework.UI.UnityEventHelper;
namespace RGBC
{
	public class MainView : UIView
    {
		class View {
//FieldsStart
		public UnityEngine.UI.Button battle;

//FieldsEnd
		public View(MainView context){
//InitComponentsStart
			battle = context.GetComponent<UnityEngine.UI.Button>("battle@sm");

//InitComponentsEnd
			}
		}
		private View view;
		[Inject]
		BattleCtrl battleCtrl;
		protected override void InitComponents()
		{
			view = new View(this);
			this.BindButton(view.battle, () => { battleCtrl.EnterGame(); });
		}
		protected override void OnLoad(){}
		protected override void OnShow(){}
		protected override void OnHide(){}
		protected override void OnClose(){}
	}
}
