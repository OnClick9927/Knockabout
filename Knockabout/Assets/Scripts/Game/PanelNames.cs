public class PanelNames
{
	public const string Battle = "Assets/Art/Game/Prefabs/UI/Battle.prefab";
	public const string LoadScene = "Assets/Art/Game/Prefabs/UI/LoadScene.prefab";
	public const string Login = "Assets/Art/Game/Prefabs/UI/Login.prefab";
	public const string Main = "Assets/Art/Game/Prefabs/UI/Main.prefab";
	public const string Tip = "Assets/Art/Game/Prefabs/UI/Tip.prefab";
	public static System.Collections.Generic.Dictionary<string, System.Type> map = new System.Collections.Generic.Dictionary<string, System.Type>()
	{
		{Battle,typeof(RGBC.BattleView)},
		{LoadScene,typeof(LoadSceneView)},
		{Login,typeof(RGBC.UI.LoginView)},
		{Main,typeof(RGBC.MainView)},
		{Tip,typeof(TipView)},
	};
}
