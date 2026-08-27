using IFramework;
using IFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using WooAsset;
using WooTween;
using static EventDefine;

public static class GameTools
{
    private class TweenBox : IDisposable
    {
        private ITweenContext context;

        public static IDisposable Create(ITweenContext context)
        {
            var box = StaticPool.Get<TweenBox>();
            box.context = context;
            return box;
        }
        public void Dispose()
        {
            if (Tween.IsRunning(this.context))
            {
                this.context.Stop();
                this.context.Recycle();
            }
            StaticPool.Set(this);
        }
    }
    public static IDisposable AsDisposable(this ITweenContext context) => TweenBox.Create(context);
    public static AsyncTask<EventDefine.LoadSceneEndArgs> LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        Events.Publish(new LoadSceneArgs(sceneName, mode));
        return Events.Wait<EventDefine.LoadSceneEndArgs>();
    }

    public static void ShowTip(string content)
        => Events.Publish(new ShowTipArgs(content));
    public static void ClearAssetCollection(string path)
        => Assets.ClearAssetCollection(path);

    public static Color Hex2Color(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;


    public static async AsyncTask SetSprite(this WidgetView view, UnityEngine.UI.Image image, string path)
    {
        if (string.IsNullOrEmpty(path))
            image.sprite = null;
        else
        {
#if UNITY_EDITOR
            if (!(view.root is UIView))
            {
                Debug.LogError($"没有正确创建 UIItem {view}");
            }

#endif
            var collection = Assets.GetAssetCollection((view.root as UIView).panel.GetPath());
            var asset = collection.Get(path, () => Assets.LoadAssetAsync<Sprite>(path));
            await asset;
            Sprite sp = (asset as WooAsset.Asset).GetAsset<Sprite>();
            image.sprite = sp;
        }
    }


    public static void ClearUI(this UIService ui)
    {
        ui.CloseWithout(
     PanelNames.LoadScene,
     PanelNames.Tip);
    }











    private static IEnumerable<Type> AllTypes { get; set; }
    public static IEnumerable<Type> GetTypes()
    {
        if (AllTypes == null)
        {
            AllTypes = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(x => x.GetTypes()).Where(x => !x.IsAbstract);
        }
        return AllTypes;
    }
    public static List<T> CreateSubTypeInstances<T>() where T : class
    {
        return typeof(T).GetSubTypes().Select(x => Activator.CreateInstance(x) as T).ToList();
    }
    public static IEnumerable<Type> GetSubTypes(this Type type)
    {

        if (type.IsInterface)
            return GetTypes().Where(x => x.GetInterfaces().Contains(type));
        return GetTypes().Where(x => x.IsSubclassOf(type));

    }
    private static MethodInfo method_0;
    private static Dictionary<Type, MethodInfo> map = new Dictionary<Type, MethodInfo>();
    private static MethodInfo GetGenMethod(Type arg)
    {
        if (method_0 == null)
        {
            method_0 = typeof(Events)
                .GetMethod($"{nameof(Events.SubscribeEvent)}", 1,
                BindingFlags.Static | BindingFlags.Public,
                null, new Type[] { typeof(object), typeof(IEventHandler) },
                null);
        }
        if (!map.TryGetValue(arg, out var result))
        {
            result = method_0.MakeGenericMethod(arg);
            map.Add(arg, result);
        }
        return result;

    }
    public static void RegisterEventHandlers(this object owner)
    {
        var list = owner.GetType().GetInterfaces()
            .Where(x => x.IsGenericType && typeof(IEventHandler).IsAssignableFrom(x));
        foreach (var item in list)
        {
            var type = item.GetGenericArguments()[0];
            var method = GetGenMethod(type);
            method.Invoke(null, new object[] { owner, owner });
        }
    }
}

