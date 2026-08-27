using ActionEditor.Nodes.BT;
using GamePlay;
using IFramework;
using IFramework.UI;
using RGBC;
using System.Collections.Generic;
using UnityEngine;

public partial class GameState_GamePlay : IGameState, IInjectAble, IEventHandler<EventDefine.GameStartArgs>
    , IEventHandler<EventDefine.RecPlayerInputsArgs>
{
    [Inject(UIServiceEx.defaultName)] UIService UI;
    bool Recon = false;
    void IGameState.Init()
    {

    }
    async void IGameState.OnEnter(IGameState exit)
    {
        Recon = false;
        if (exit is GameState_TryRecon) return;

        this.RegisterEventHandlers();
        await GameTools.LoadScene(GameContext.gameData.level.Scene);
        UI.ClearUI();

        InitGame();
    }

    void IGameState.OnExit(IGameState enter)
    {
        if (enter is GameState_TryRecon)
        {
            Recon = true;
        }
        else
        {
            QuitGame();
        }
    }
    void IGameState.Quit() => QuitGame();
    void QuitGame()
    {
        if (world == null) return;
        Services.Remove(world);
        Services.Remove(this);
        assets.Release();
        world = null;
    }

    void IGameState.Update()
    {
        if (Recon) return;
        if (input == null) return;
        world_timer += Time.deltaTime;
        if (world_timer >= GameContext.logicDeltaTime)
        {
            (world as IUpdate).Update();
            world_timer = 0;
        }
        UpdateViews();
    }




    private IGameWorld world;
    public PlayerInput input { get; private set; }
    private LocalModeServer server;
    private float world_timer;

    private int _speed;
    public int speed
    {
        get => _speed; set
        {
            if (_speed == value) return;
            _speed = value;
            GameContext.state.SetSpeed(value);
        }
    }

    public void ResetSpeed() => GameContext.state.ResetSpeed();


    public GameAssets assets => game.gameAssets;

    public async void InitGame()
    {
        Services.Add(this);
        await assets.Prepare();
        PrepareAssets();

        await UI.Show(PanelNames.Battle);
        world = new GameWorld();
        server = new LocalModeServer();
        (world as GameWorld).Init();
        speed = 1;
    }

    void IEventHandler<EventDefine.GameStartArgs>.OnEvent(EventDefine.GameStartArgs message)
    {
        input = new();
        world.StartGame();
    }

    void IEventHandler<EventDefine.RecPlayerInputsArgs>.OnEvent(EventDefine.RecPlayerInputsArgs message)
    {
        world.OnRecPlayerInput(message.inputs);
    }


}
partial class GameState_GamePlay : GameHelper
{
    void GameHelper.SendPlayerInputToServer()
    {
        input.frame = GameContext.state.currentFrame;
        input.guid = GameContext.localPlayer;
        switch (GameContext.GameType)
        {
            case GameType.Local:
                server.Rec(input);
                break;
            default:
                break;
        }
        input = new();

    }
    void GameHelper.Error(string msg) => UnityEngine.Debug.LogError($"<color=#00ffee>GamePlay:</color> {msg}");
    void GameHelper.Log(string msg) => UnityEngine.Debug.Log($"<color=#00ffee>GamePlay:</color> {msg}");
    private Dictionary<int, BuffAsset.Buff> buffs;
    private Dictionary<int, Ability> ability;
    private Dictionary<int, ActorModifyAsset.Modify> actorModify;
    private readonly Dictionary<string, BTTree> btCache = new();
    private void PrepareAssets()
    {
        btCache.Clear();
        {
            var buff_txt = assets.FindAsset(assets.buff.path).GetAsset<TextAsset>();
            var asset = BuffAsset.FromBytes(buff_txt.bytes);
            buffs = asset.buffs;
        }
        {
            var ability_txt = assets.FindAsset(assets.ability.path).GetAsset<TextAsset>();
            var asset = AbilityAsset.FromBytes(ability_txt.bytes);
            ability = asset.abilitys;
        }
        {
            var actorModify_txt = assets.FindAsset(assets.actorModify.path).GetAsset<TextAsset>();
            var asset = ActorModifyAsset.FromBytes(actorModify_txt.bytes);
            actorModify = asset.buffs;
        }
    }
    SkillAsset GameHelper.GetSkillAsset(int skill)
    {
        return default;
    }

    BuffAsset.Buff GameHelper.LoadBuff(int id) => buffs.TryGetValue(id, out var res) ? res : default;

    Ability GameHelper.LoadAbility(int ability_id) => ability.TryGetValue(ability_id, out var res) ? res : default;

    ActorModifyAsset.Modify GameHelper.Load(int id) => actorModify.TryGetValue(id, out var res) ? res : default;

    public ActorBTAsset LoadRoleBTAsset(Luban.Role roleCfg, Luban.RoleLev roleLevCfg)
    {
        string path = roleCfg.BT;
        if (!string.IsNullOrEmpty(roleLevCfg.BT))
            path = roleLevCfg.BT;
        BTTree.loader = _Load;
        if (btCache.TryGetValue(path, out var cached))
            return cached as ActorBTAsset;
        ActorBTAsset asset = _Load(path) as ActorBTAsset;
        asset.PrepareForRuntime();
        return asset;
    }
    private BTTree _Load(string path)
    {
        if (btCache.TryGetValue(path, out var cached))
            return cached;
        var asset = assets.FindAsset(path);
        var bytes = asset.GetAsset<TextAsset>().bytes;
        var tree = ActorBTAsset.FromBytes(typeof(ActorBTAsset), bytes) as ActorBTAsset;
        btCache.Add(path, tree);
        return tree;
    }
}
partial class GameState_GamePlay : IViewService
{
    [Inject] GGame game;
    [Inject] IGameObjectPool pool;
    internal void Input_UseCard(int index, int card_id)
    {
        input.type = PlayerInput.InputType.UseCard;
        input.Card_index = index;
        input.Card_id = card_id;
    }


    private List<ActorView> views = new();
    private Dictionary<long, ActorView> map = new Dictionary<long, ActorView>();
    private Dictionary<long, HealthBarView> map_health = new Dictionary<long, HealthBarView>();

    private ActorView Find(long target) => map.TryGetValue(target, out ActorView view) ? view : null;
    private T Find<T>(long target) where T : ActorView => Find(target) as T;
    void IViewService.FindOrCreateActorView(Actor actor)
    {
        ActorView view = null;
        var id = actor.uid;
        view = Find(id);
        var type = actor.type;
        if (view == null)
        {
            view = Create(actor);
            if (view == null) return;
            views.Add(view);
            map[id] = view;
        }
        view.BindActor(actor);
        view.Init(id, type);
    }
    void IViewService.DestroyUseLessActorView()
    {

        for (int i = views.Count - 1; i >= 0; i--)
        {
            var dispose = views[i];
            var view = dispose;
            var target = view.target;
            if (Services.actor.Find(target) == null)
                _DestroyActorView(target, true, i).Coroutine();

        }
    }
    private async AsyncTask _DestroyActorView(long target, bool Immediate, int index)
    {
        if (index == -1)
        {
            for (int i = 0; i < views.Count; i++)
                if (views[i].target == target)
                {
                    index = i;
                    break;
                }
        }
        if (map_health.Remove(target, out var bar))
            pool.Set(bar);
        map.Remove(target);
        var view = views[index];
        views.RemoveAt(index);

        await (view as ActorView).Destroy(Immediate);

        pool.Set(view as ActorView);
    }

    private void UpdateViews()
    {
        for (int i = 0; i < views.Count; i++)
            views[i].Update();
    }


    private ActorView Create(Actor actor)
    {
        if (actor is PlayerActor)
            return pool.Get<HouseView>(assets.house.path).result;
        if (actor is RoleActor role)
        {
            return pool.Get<RoleView>(role.roleCfg.Prefab).result;
        }

        return null;
    }

    async void SyncHP(Actor actor)
    {
        var view = Find(actor.uid);
        while (view == null)
        {
            await AsyncTask.NextFrame();
            view = Find(actor.uid);
        }

        if (actor is PlayerActor player)
        {
            var houseView = view as HouseView;
            if (!map_health.TryGetValue(player.uid, out var bar))
            {
                bar = await pool.Get<HealthBarView>(assets.healthBar.path);
                map_health.Add(player.uid, bar);
                bar.transform.SetParent(houseView.healthBarPos);
                bar.transform.localPosition = Vector3.zero;
            }
            float hp = player.property.hp;
            float max = player.property.maxHp;
            bar.SetHp(hp, max);
        }
        if (actor is RoleActor role)
        {
            var houseView = Find(role.uid) as RoleView;
            if (!map_health.TryGetValue(role.uid, out var bar))
            {
                bar = await pool.Get<HealthBarView>(assets.healthBar.path);
                map_health.Add(role.uid, bar);
                bar.transform.SetParent(houseView.healthBarPos);
                bar.transform.localPosition = Vector3.zero;
            }
            float hp = role.property.hp;
            float max = role.property.maxHp;
            bar.SetHp(hp, max);
        }
    }
    void SyncTransform(Actor actor)
    {
        var view = Find(actor.uid);
        view.SyncTransform();
    }
    void SyncStatus(Actor actor, OnTagChangeEvent tags)
    {
        if (tags.tag == Tags.Dead && tags.add)
        {
            var view = Find(actor.uid);
            view.OnDead();
            _DestroyActorView(actor.uid, false, -1).Coroutine();
        }
    }
    void IViewService.OnActorEvent(Actor actor, IActorEvent eve)
    {
        switch (eve)
        {
            case OnPropertyChangedEvent prop when prop.type == PropertyType.HP || prop.type == PropertyType.MaxHP:
                SyncHP(actor);
                break;
            case OnTransformChangeEvent:
                SyncTransform(actor);
                break;
            case OnTagChangeEvent tags:
                SyncStatus(actor, tags);
                break;
            case OnInitCardsEvent:
                Find<HouseView>(actor.uid).SyncHandCardFast();
                break;
            case OnAddCardEvent add:
                Find<HouseView>(actor.uid).OnAddCardByPlayer(add);
                break;
            case OnUseCardEvent use:
                Find<HouseView>(actor.uid).OnUseCard(use);
                break;
            default:
                break;
        }
        //if (eve is OnPropertyChangedEvent prop) {
        //    if (prop.type== PropertyType.HP || prop.type== PropertyType.MaxHP)
        //    {
        //        SyncHP(actor);
        //    }

        //}
        if (true)
        {

        }
    }
}
