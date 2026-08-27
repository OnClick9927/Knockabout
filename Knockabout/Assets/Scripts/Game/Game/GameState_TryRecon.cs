using AOT;
using IFramework;
using IFramework.UI;
using Proto;
using System;
using UnityEngine;

public class GameState_TryRecon : IGameState, IEventHandler<HeartResp>
{
    //private static int connindex = 0;
#if UNITY_EDITOR
    private const int MaxConnCount = 1;
#else
        private const int MaxConnCount = 5;
#endif
    //public bool IsOutOfConCount => connindex >= MaxConnCount;
    private static int SendGap = 6;
    private static int ReceiveGap = 12;

    private float _sendTime;
    private void CalcNextSendTime() => _sendTime = Time.time + SendGap;
    private System.DateTime _lastLime;
    private void SetRecTime() => _lastLime = DateTime.Now;
    void IEventHandler<HeartResp>.OnEvent(HeartResp message)
    {
        SetRecTime();
    }

    [Inject] NetSession session;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    [Inject] IGameStateService stateService;
    [Inject] IPrefService prefService;
    [Inject] UserCtrl userCtrl;

    private IGameState _current;
    void IGameState.Init()
    {
        this.RegisterEventHandlers();
        if (!AOTDefine.G.LocalTestMode)
            Game.BindUpdate(HeatBeatCheck);
        stateService.ListenStateChange((exit, enter) =>
        {
            _current = enter;
        });
    }
    private void HeatBeatCheck()
    {
        var state = _current;
        if (state == null || state == this || state is GameState_Login) return;

        var sec = (DateTime.Now - _lastLime).Seconds;
        if (sec > ReceiveGap)
        {
            stateService.SwitchState(this);
            return;
        }

        if (session == null || !session.IsConnected) return;
        if (_sendTime > Time.time)
        {
            CalcNextSendTime();
            session.Send(new HeartReq());
        }

    }

    async void IGameState.OnEnter(IGameState exit)
    {
        Log.L("心跳超时 ，准备重新链接");

        for (int i = 0; i < MaxConnCount; i++)
        {
            Log.L("尝试连接  {0}/{1}", i, MaxConnCount);
            session.Disconnect();
            var result = await session.Connect();
            if (result)
            {
                await userCtrl.Relogin();
                stateService.SwitchState(exit);
                return;
            }
        }
        session.Disconnect();
        Log.L("重连接 失败");
        prefService.SaveAll();
        stateService.SwitchState<GameState_Login>();

        UI.AcceptRayCast();
    }




    void IGameState.Quit()
    {
    }

    void IGameState.OnExit(IGameState enter)
    {

    }

    void IGameState.Update()
    {

    }
    public void OnBeginConnect()
    {
        UI.RefuseRayCast();

    }
    internal void OnEndConnect(bool succ)
    {
        if (succ)
            UI.AcceptRayCast();
        CalcNextSendTime();
        SetRecTime();
    }
    public bool IsLogIgnoreMessage(IMessage msg)
    {
        if (msg is HeartReq) return true;
        if (msg is HeartResp) return true;

        return false;
    }

    internal void ShowResponseErr(BaseResp resp)
    {
        GameTools.ShowTip($"{resp.GetType()} : {resp.code}");
    }
}