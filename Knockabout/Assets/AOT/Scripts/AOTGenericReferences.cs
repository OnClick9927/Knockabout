using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"IFramework.dll",
		"System.Core.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"WooAsset.dll",
		"WooLocalization.dll",
		"WooTween.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }}

	// {{ AOT generic types
	// IFramework.AsyncTask<EventDefine.LoadSceneEndArgs>
	// IFramework.AsyncTask<byte>
	// IFramework.AsyncTask<object>
	// IFramework.AsyncTaskAwaiter<EventDefine.LoadSceneEndArgs>
	// IFramework.AsyncTaskAwaiter<byte>
	// IFramework.AsyncTaskAwaiter<object>
	// IFramework.AsyncTaskMethodBuilder<byte>
	// IFramework.AsyncTaskMethodBuilder<object>
	// IFramework.Events.<>c__DisplayClass18_0<EventDefine.LoadSceneEndArgs>
	// IFramework.IAwaiter<EventDefine.LoadSceneEndArgs>
	// IFramework.IAwaiter<byte>
	// IFramework.IEventHandler<EventDefine.LoadSceneArgs>
	// IFramework.IEventHandler<EventDefine.ShowTipArgs>
	// IFramework.MonoSingleton<object>
	// IFramework.ObjectPool.<>c<object>
	// IFramework.ObjectPool<object>
	// System.Action<UnityEngine.Rendering.ScriptableRenderContext,object>
	// System.Action<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Action<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Action<int>
	// System.Action<object,UnityEngine.Color>
	// System.Action<object,UnityEngine.Rect>
	// System.Action<object,UnityEngine.Vector2>
	// System.Action<object,UnityEngine.Vector3>
	// System.Action<object,UnityEngine.Vector4>
	// System.Action<object,float>
	// System.Action<object,object>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.ArraySortHelper<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.Comparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.Dictionary<int,float>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Color>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Rect>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Vector2>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Vector4>
	// System.Collections.Generic.EqualityComparer<WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,WindowsDesktopWindow.CameraState>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.ICollection<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.IComparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,WindowsDesktopWindow.CameraState>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerable<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,WindowsDesktopWindow.CameraState>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerator<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.IList<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyList<object>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int>,object>
	// System.Collections.Generic.KeyValuePair<int,float>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.KeyValuePair<object,float>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.List.Enumerator<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.List<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.Generic.ObjectComparer<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<int,int>>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Color>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Rect>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Vector2>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Vector4>
	// System.Collections.Generic.ObjectEqualityComparer<WindowsDesktopWindow.CameraState>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Collections.ObjectModel.ReadOnlyCollection<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Comparison<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<int,int,object>
	// System.Func<object,UnityEngine.Color>
	// System.Func<object,UnityEngine.Rect>
	// System.Func<object,UnityEngine.Vector2>
	// System.Func<object,UnityEngine.Vector3>
	// System.Func<object,UnityEngine.Vector4>
	// System.Func<object,byte>
	// System.Func<object,float>
	// System.Func<object,int,int,int,object>
	// System.Func<object,int,int,object>
	// System.Func<object,int,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<object>
	// System.Lazy<object>
	// System.Linq.Enumerable.<RepeatIterator>d__117<object>
	// System.Linq.Enumerable.<SelectManyIterator>d__17<object,object>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,object>
	// System.Linq.Enumerable.WhereSelectListIterator<object,object>
	// System.Predicate<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>>
	// System.Predicate<WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.ValueTuple<int,int>
	// UnityEngine.Events.InvokableCall<object>
	// UnityEngine.Events.UnityAction<object>
	// UnityEngine.Events.UnityEvent<object>
	// WooAsset.AssetReference<object>
	// WooAsset.AssetsAsyncSupport.AssetOperationAwaiter<object>
	// WooAsset.AssetsAsyncSupport.IAwaiter<object>
	// WooAsset.BundleAssetHandle<object>
	// WooLocalization.ActorAsset<float>
	// WooLocalization.ActorAsset<object>
	// WooLocalization.IActorContext<float>
	// WooLocalization.IActorContext<object>
	// WooLocalization.LocalizationActor<object>
	// WooLocalization.LocalizationBehavior<object>
	// WooLocalization.LocalizationGraphic<object>
	// WooLocalization.LocalizationMapActor<object,float>
	// WooLocalization.LocalizationMapActor<object,object>
	// WooLocalization.SerializableDictionary.Enumerator<object,float>
	// WooLocalization.SerializableDictionary.Enumerator<object,object>
	// WooLocalization.SerializableDictionary.KeyCollection<object,float>
	// WooLocalization.SerializableDictionary.KeyCollection<object,object>
	// WooLocalization.SerializableDictionary.KeyEnumerator<object,float>
	// WooLocalization.SerializableDictionary.KeyEnumerator<object,object>
	// WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,float>
	// WooLocalization.SerializableDictionary.SerializableKeyValuePair<object,object>
	// WooLocalization.SerializableDictionary.ValueCollection<object,float>
	// WooLocalization.SerializableDictionary.ValueCollection<object,object>
	// WooLocalization.SerializableDictionary.ValueEnumerator<object,float>
	// WooLocalization.SerializableDictionary.ValueEnumerator<object,object>
	// WooLocalization.SerializableDictionary<object,float>
	// WooLocalization.SerializableDictionary<object,object>
	// WooLocalization.TextValueActor_Base<object>
	// WooTween.ArrayBuffer<UnityEngine.Color>
	// WooTween.ArrayBuffer<UnityEngine.Rect>
	// WooTween.ArrayBuffer<UnityEngine.Vector2>
	// WooTween.ArrayBuffer<UnityEngine.Vector3>
	// WooTween.ArrayBuffer<UnityEngine.Vector4>
	// WooTween.ArrayBuffer<float>
	// WooTween.TweenComponentActor<UnityEngine.Color,object>
	// WooTween.TweenComponentActor<UnityEngine.Rect,object>
	// WooTween.TweenComponentActor<UnityEngine.Vector2,object>
	// WooTween.TweenComponentActor<UnityEngine.Vector3,object>
	// WooTween.TweenComponentActor<UnityEngine.Vector4,object>
	// WooTween.TweenComponentActor<float,object>
	// WooTween.TweenContext<UnityEngine.Color,object>
	// WooTween.TweenContext<UnityEngine.Rect,object>
	// WooTween.TweenContext<UnityEngine.Vector2,object>
	// WooTween.TweenContext<UnityEngine.Vector3,object>
	// WooTween.TweenContext<UnityEngine.Vector4,object>
	// WooTween.TweenContext<float,object>
	// WooTween.TweenGroupComponentActor<object>
	// WooTween.ValueCalculator<UnityEngine.Color>
	// WooTween.ValueCalculator<UnityEngine.Rect>
	// WooTween.ValueCalculator<UnityEngine.Vector2>
	// WooTween.ValueCalculator<UnityEngine.Vector3>
	// WooTween.ValueCalculator<UnityEngine.Vector4>
	// WooTween.ValueCalculator<float>
	// }}

	public void RefMethods()
	{
		// System.Void IFramework.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,GameTools.<SetSprite>d__6>(object&,GameTools.<SetSprite>d__6&)
		// System.Void IFramework.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,GGame.<IFramework-IGameObjectPoolAsset-LoadAsset>d__26>(object&,GGame.<IFramework-IGameObjectPoolAsset-LoadAsset>d__26&)
		// System.Void IFramework.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,GGame.<IFramework-UI-IUIDelegate-LoadPanelAsync>d__45>(object&,GGame.<IFramework-UI-IUIDelegate-LoadPanelAsync>d__45&)
		// System.Void IFramework.AsyncTaskMethodBuilder.Start<GameTools.<SetSprite>d__6>(GameTools.<SetSprite>d__6&)
		// System.Void IFramework.AsyncTaskMethodBuilder<byte>.Start<UserCtrl.<Login>d__5>(UserCtrl.<Login>d__5&)
		// System.Void IFramework.AsyncTaskMethodBuilder<object>.Start<GGame.<IFramework-IGameObjectPoolAsset-LoadAsset>d__26>(GGame.<IFramework-IGameObjectPoolAsset-LoadAsset>d__26&)
		// System.Void IFramework.AsyncTaskMethodBuilder<object>.Start<GGame.<IFramework-UI-IUIDelegate-LoadPanelAsync>d__45>(GGame.<IFramework-UI-IUIDelegate-LoadPanelAsync>d__45&)
		// System.Void IFramework.Events.Notify<EventDefine.LoadSceneEndArgs>(EventDefine.LoadSceneEndArgs)
		// System.Void IFramework.Events.Notify<EventDefine.LoadSceneEndArgs>(string,EventDefine.LoadSceneEndArgs)
		// System.Void IFramework.Events.Publish<EventDefine.LoadSceneArgs>(EventDefine.LoadSceneArgs)
		// System.Void IFramework.Events.Publish<EventDefine.ShowTipArgs>(EventDefine.ShowTipArgs)
		// System.Void IFramework.Events.Publish<EventDefine.UITopVisibleChange>(EventDefine.UITopVisibleChange)
		// IFramework.AsyncTask<EventDefine.LoadSceneEndArgs> IFramework.Events.Wait<EventDefine.LoadSceneEndArgs>(IFramework.CancellationToken)
		// IFramework.AsyncTask<EventDefine.LoadSceneEndArgs> IFramework.Events.Wait<EventDefine.LoadSceneEndArgs>(string,IFramework.CancellationToken)
		// object IFramework.GameObjectView.GetComponent<object>(string)
		// IFramework.IServiceProvider IFramework.GameStateServiceEx.EnterState<object>(IFramework.IServiceProvider)
		// bool IFramework.GameStateServiceEx.SwitchState<object>(IFramework.IGameStateService)
		// object IFramework.IDisposableCollection.AddTo<object>(object,object)
		// object IFramework.UI.UnityEventHelper.Allocate<object>()
		// object IFramework.UI.UnityEventHelper.Bind<object>(object,System.Action,System.Action)
		// object IFramework.UI.UnityEventHelper.Bind<object>(object,UnityEngine.Events.UnityEvent,UnityEngine.Events.UnityAction)
		// object IFramework.UI.UnityEventHelper.Bind<object>(object,UnityEngine.Events.UnityEvent<object>,UnityEngine.Events.UnityAction<object>)
		// object IFramework.UI.UnityEventHelper.BindButton<object>(object,UnityEngine.UI.Button,UnityEngine.Events.UnityAction)
		// byte[] System.Array.Empty<byte>()
		// object[] System.Array.Empty<object>()
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object,System.Collections.Generic.IEqualityComparer<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Repeat<object>(object,int)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.RepeatIterator<object>(object,int)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.SelectMany<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,System.Collections.Generic.IEnumerable<object>>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.SelectManyIterator<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,System.Collections.Generic.IEnumerable<object>>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<object>.Select<object>(System.Func<object,object>)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,GGame.<Startup>d__7>(object&,GGame.<Startup>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,GameState_Login.<IFramework-IGameState-OnEnter>d__5>(object&,GameState_Login.<IFramework-IGameState-OnEnter>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,GameState_Main.<IFramework-IGameState-OnEnter>d__2>(object&,GameState_Main.<IFramework-IGameState-OnEnter>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<object,RGBC.UI.LoginView.<<OnLoad>b__6_0>d>(object&,RGBC.UI.LoginView.<<OnLoad>b__6_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GGame.<Startup>d__7>(GGame.<Startup>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GameState_Login.<IFramework-IGameState-OnEnter>d__5>(GameState_Login.<IFramework-IGameState-OnEnter>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<GameState_Main.<IFramework-IGameState-OnEnter>d__2>(GameState_Main.<IFramework-IGameState-OnEnter>d__2&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<RGBC.UI.LoginView.<<OnLoad>b__6_0>d>(RGBC.UI.LoginView.<<OnLoad>b__6_0>d&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<RGBC.UI.LoginView.<OnLoad>d__6>(RGBC.UI.LoginView.<OnLoad>d__6&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// System.IntPtr System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate<object>(object)
		// int System.Runtime.InteropServices.Marshal.SizeOf<WindowsDesktopWindow.MonitorInfo>()
		// int System.Runtime.InteropServices.Marshal.SizeOf<WindowsDesktopWindow.NotifyIconData>()
		// object UnityEngine.Component.GetComponent<object>()
		// bool UnityEngine.Component.TryGetComponent<object>(object&)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// bool UnityEngine.GameObject.TryGetComponent<object>(object&)
		// object UnityEngine.JsonUtility.FromJson<object>(string)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Vector3,UnityEngine.Quaternion,UnityEngine.Transform)
		// object WooAsset.Asset.GetAsset<object>()
		// WooAsset.Asset WooAsset.Assets.LoadAssetAsync<object>(string)
		// WooAsset.AssetsAsyncSupport.IAwaiter<object> WooAsset.AssetsAsyncSupport.GetAwaiter<object>(object)
		// WooTween.ITweenContext<UnityEngine.Color,object> WooTween.Tween.Allocate<UnityEngine.Color,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Rect,object> WooTween.Tween.Allocate<UnityEngine.Rect,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector2,object> WooTween.Tween.Allocate<UnityEngine.Vector2,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.Allocate<UnityEngine.Vector3,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector4,object> WooTween.Tween.Allocate<UnityEngine.Vector4,object>(bool)
		// WooTween.ITweenContext<float,object> WooTween.Tween.Allocate<float,object>(bool)
		// WooTween.TweenContext<UnityEngine.Color,object> WooTween.Tween.AsInstance<UnityEngine.Color,object>(WooTween.ITweenContext<UnityEngine.Color,object>)
		// WooTween.TweenContext<UnityEngine.Rect,object> WooTween.Tween.AsInstance<UnityEngine.Rect,object>(WooTween.ITweenContext<UnityEngine.Rect,object>)
		// WooTween.TweenContext<UnityEngine.Vector2,object> WooTween.Tween.AsInstance<UnityEngine.Vector2,object>(WooTween.ITweenContext<UnityEngine.Vector2,object>)
		// WooTween.TweenContext<UnityEngine.Vector3,object> WooTween.Tween.AsInstance<UnityEngine.Vector3,object>(WooTween.ITweenContext<UnityEngine.Vector3,object>)
		// WooTween.TweenContext<UnityEngine.Vector4,object> WooTween.Tween.AsInstance<UnityEngine.Vector4,object>(WooTween.ITweenContext<UnityEngine.Vector4,object>)
		// WooTween.TweenContext<float,object> WooTween.Tween.AsInstance<float,object>(WooTween.ITweenContext<float,object>)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoArray<UnityEngine.Vector3,object>(object,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,UnityEngine.Vector3[],bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoBezier<UnityEngine.Vector3,object>(object,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,UnityEngine.Vector3[],bool,bool)
		// WooTween.ITweenContext<UnityEngine.Color,object> WooTween.Tween.DoGoto<UnityEngine.Color,object>(object,UnityEngine.Color,UnityEngine.Color,float,System.Func<object,UnityEngine.Color>,System.Action<object,UnityEngine.Color>,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Rect,object> WooTween.Tween.DoGoto<UnityEngine.Rect,object>(object,UnityEngine.Rect,UnityEngine.Rect,float,System.Func<object,UnityEngine.Rect>,System.Action<object,UnityEngine.Rect>,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector2,object> WooTween.Tween.DoGoto<UnityEngine.Vector2,object>(object,UnityEngine.Vector2,UnityEngine.Vector2,float,System.Func<object,UnityEngine.Vector2>,System.Action<object,UnityEngine.Vector2>,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoGoto<UnityEngine.Vector3,object>(object,UnityEngine.Vector3,UnityEngine.Vector3,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector4,object> WooTween.Tween.DoGoto<UnityEngine.Vector4,object>(object,UnityEngine.Vector4,UnityEngine.Vector4,float,System.Func<object,UnityEngine.Vector4>,System.Action<object,UnityEngine.Vector4>,bool,bool)
		// WooTween.ITweenContext<float,object> WooTween.Tween.DoGoto<float,object>(object,float,float,float,System.Func<object,float>,System.Action<object,float>,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoJump<UnityEngine.Vector3,object>(object,UnityEngine.Vector3,UnityEngine.Vector3,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,UnityEngine.Vector3,int,float,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector2,object> WooTween.Tween.DoPunch<UnityEngine.Vector2,object>(object,UnityEngine.Vector2,UnityEngine.Vector2,float,System.Func<object,UnityEngine.Vector2>,System.Action<object,UnityEngine.Vector2>,UnityEngine.Vector2,int,float,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoPunch<UnityEngine.Vector3,object>(object,UnityEngine.Vector3,UnityEngine.Vector3,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,UnityEngine.Vector3,int,float,bool,bool)
		// WooTween.ITweenContext<float,object> WooTween.Tween.DoPunch<float,object>(object,float,float,float,System.Func<object,float>,System.Action<object,float>,float,int,float,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector2,object> WooTween.Tween.DoShake<UnityEngine.Vector2,object>(object,UnityEngine.Vector2,UnityEngine.Vector2,float,System.Func<object,UnityEngine.Vector2>,System.Action<object,UnityEngine.Vector2>,UnityEngine.Vector2,int,float,bool,bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.Tween.DoShake<UnityEngine.Vector3,object>(object,UnityEngine.Vector3,UnityEngine.Vector3,float,System.Func<object,UnityEngine.Vector3>,System.Action<object,UnityEngine.Vector3>,UnityEngine.Vector3,int,float,bool,bool)
		// WooTween.ITweenContext<float,object> WooTween.Tween.DoShake<float,object>(object,float,float,float,System.Func<object,float>,System.Action<object,float>,float,int,float,bool,bool)
		// object WooTween.Tween.Recycle<object>(object)
		// object WooTween.Tween.Run<object>(object)
		// object WooTween.Tween.Stop<object>(object)
		// WooTween.ITweenContext<UnityEngine.Color,object> WooTween.TweenScheduler.AllocateContext<UnityEngine.Color,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Rect,object> WooTween.TweenScheduler.AllocateContext<UnityEngine.Rect,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector2,object> WooTween.TweenScheduler.AllocateContext<UnityEngine.Vector2,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector3,object> WooTween.TweenScheduler.AllocateContext<UnityEngine.Vector3,object>(bool)
		// WooTween.ITweenContext<UnityEngine.Vector4,object> WooTween.TweenScheduler.AllocateContext<UnityEngine.Vector4,object>(bool)
		// WooTween.ITweenContext<float,object> WooTween.TweenScheduler.AllocateContext<float,object>(bool)
		// string string.Join<object>(string,System.Collections.Generic.IEnumerable<object>)
		// string string.JoinCore<object>(System.Char*,int,System.Collections.Generic.IEnumerable<object>)
	}
}
