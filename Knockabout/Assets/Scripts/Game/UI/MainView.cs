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

//FieldsEnd
		public View(MainView context){
//InitComponentsStart

//InitComponentsEnd
			}
		}
		private View view;

		protected override void InitComponents()
		{
			view = new View(this);
		
		}
		protected override void OnLoad(){}
		protected override void OnShow(){}
		protected override void OnHide(){}
		protected override void OnClose(){}
	}
}
