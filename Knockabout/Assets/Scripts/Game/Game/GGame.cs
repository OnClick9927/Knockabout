using ActionBuffer;
using AOT;
using IFramework;
using IFramework.UI;
using Luban;
using System;
using System.Collections.Generic;
using UnityEngine;
using WooAsset;
using WooLocalization;


public partial class GGame : Game, IInjectAble
{
    public Canvas canvas;
    public UILayerData layer;
    public LocalizationData LocalizationData;

    public Camera UICamera;
    public AssetReference<TextAsset> uiCollect = new AssetReference<TextAsset>();
    public GameAssets gameAssets;
    [Inject] IPrefService prefService;
    [Inject] IGameStateService stateService;
    private const string address = "ws://127.0.0.1:5002/ws";
    protected async override void Startup()
    {
        Log.L("热更新逻辑开始");
        AssetsGroupOperation op = await Assets.PrepareAssetsByTag(AOT.AOTDefine.ConfigAssetTag);
        var panelCollection = (op.FindAsset(uiCollect.path) as WooAsset.Asset).GetAsset<TextAsset>().text;
        this.UseLuban(op)
            .UseValues()
            .UsePref(this, this)
            .UseMvc(GameTools.CreateSubTypeInstances<ModelBase>(), GameTools.CreateSubTypeInstances<CtrlBase>())
            .UseState(GameTools.CreateSubTypeInstances<IGameState>())
            .Use(new NetSession(address));

        this.Values().Inject(this);

        var uiService = this.UseLocalization(LocalizationData)
                .UseGameObjectPool(this)
                .UseAudio(this, this)
                .UseUI(layer, JsonUtility.FromJson<PanelCollection>(panelCollection), new ViewBridge(PanelNames.map), this, canvas);

        await uiService.Show(PanelNames.LoadScene);
        uiService.Show(PanelNames.Tip).Coroutine();

        prefService.SetContext(new PrefContext<PrefBeforeLogin>());
        prefService.SetContext(new PrefContext<PrefAfterLogin>());
        this.EnterLocalization(this, AOTDefine.G.LocalizationType)
            .EnterAudio()
            .EnterMvc()
            .EnterService<NetSession>()
            .EnterState<GameState_Login>();
    }


    protected override void OnQuit()
    {
        base.OnQuit();
        stateService.SwitchState<GameState_GameQuit>();
    }


}
partial class GGame
{
    public IServiceCollection UseLuban(AssetsGroupOperation op)
    {
        Configs.Init(new Tables((file) =>
        {
            string path = Configs.GetConfigFile(file);
            var asset = op.FindAsset(path) as WooAsset.Asset;
            var txt = asset.GetAsset<TextAsset>();
            return ByteBuf.Wrap(txt.bytes);

        }));
        op.Release();
        return this;
    }
    public Game UseLocalization(LocalizationData data)
    {
        Localization.SetContext(data);
        return this;
    }
    public Game EnterLocalization(ILocalizationPrefRecorder recorder, string type)
    {
        Localization.SetRecorder(recorder);
        Localization.SetDefaultLocalizationType(type);
        return this;
    }
}

partial class GGame : ILocalizationPrefRecorder,
    IAudioHelper, IAudioConfig, IGameObjectPoolAsset,
    IPrefConverter, IPrefLoader, IUIDelegate
{

    LocalizationPref ILocalizationPrefRecorder.Read() => prefService.Load<LocalizationPref>();
    void ILocalizationPrefRecorder.Write(LocalizationPref pref) => prefService.Save(pref);





    AudioPref IAudioHelper.Read() => prefService.Load<AudioPref>();
    void IAudioHelper.Write(AudioPref pref) => prefService.Save(pref);
    private Dictionary<string, WooAsset.Asset> audios = new Dictionary<string, WooAsset.Asset>();
    bool IAudioHelper.IsDone(string path)
   => audios.TryGetValue(path, out var clip) ? clip.isDone : false;
    void IAudioHelper.Load(string path)
    {
        if (!audios.TryGetValue(path, out var clip))
        {
            clip = Assets.LoadAssetAsync(path);
            audios[path] = clip;
        }
    }

    AudioClip IAudioHelper.GetClip(string path)
        => audios.TryGetValue(path, out var clip) ? clip.GetAsset<AudioClip>() : default;

    void IAudioHelper.Release(string path)
    {
        if (audios.Remove(path, out var clip))
            Assets.Release(path);
    }

    private SoundData GetSound(int id) => Configs.GetSound().GetOrDefault(id);
    int IAudioConfig.GetSoundChannel(int sound_id) => (int)GetSound(sound_id).Channel;

    bool IAudioConfig.GetSoundLoop(int sound_id) => GetSound(sound_id).Loop;

    string IAudioConfig.GetSoundPath(int sound_id) => GetSound(sound_id).Path;
    SoundCoverType IAudioConfig.GetSoundCover(int sound_id) => GetSound(sound_id).Cover ? SoundCoverType.Other : SoundCoverType.None;

    async AsyncTask<GameObject> IGameObjectPoolAsset.LoadAsset(string key)
    {
        var asset = await Assets.LoadAssetAsync(key);
        var go = asset.GetAsset<GameObject>();
        return go;
    }

    void IGameObjectPoolAsset.ReleaseAsset(string key, GameObject asset)
    {
        Assets.Release(key);
    }
    BuffSettings settings = new BuffSettings()
    {
        PrettyPrint = true,
    };
    object IPrefConverter.FromString(Type type, string str) => BuffSerializer.FromJson(str, type, settings);
    string IPrefConverter.ToString(object obj, Type type) => ActionBuffer.BuffSerializer.ToJson(obj, settings);

    string IPrefLoader.Load(string key) => PlayerPrefs.GetString(key);

    void IPrefLoader.Save(string key, string json) => PlayerPrefs.SetString(key, json);






    void IUIDelegate.OnFullScreenCount(bool hide, int count)
    {
    }

    void IUIDelegate.OnLayerTopChange(int layer, string top)
    {
    }


    void IUIDelegate.OnPanelClose(string path)
    {
        GameTools.ClearAssetCollection(path);
    }

    void IUIDelegate.OnPanelHide(string path)
    {

    }

    void IUIDelegate.OnPanelLoad(string path)
    {

    }

    void IUIDelegate.OnPanelShow(string path)
    {

    }

    void IUIDelegate.OnVisibleChange(string path, bool visible)
    {
    }

    void IUIDelegate.OnLayerTopShowChange(int layer, string path)
    {
        Events.Publish(new EventDefine.UITopVisibleChange(path, layer));

    }

    void IUIDelegate.OnTopShowChange(int layer, string path)
    {
    }

    void IUIDelegate.OnClosePanelAsync(string path)
    {
    }

    void IUIDelegate.OnHidePanelAsync(string path)
    {
    }

    void IUIDelegate.OnShowPanelRequest(string path)
    {
    }

    async AsyncTask<UIPanel> IUIDelegate.LoadPanelAsync(RectTransform parent, PanelCollection.Data data)
    {
        var asset = await Assets.InstantiateAsync(data.path, parent);
        return asset.gameObject.GetComponent<UIPanel>();

    }

    void IUIDelegate.DestroyPanel(GameObject gameObject)
    {
        gameObject.SetActive(false);
        GameObject.Destroy(gameObject);
    }


}

