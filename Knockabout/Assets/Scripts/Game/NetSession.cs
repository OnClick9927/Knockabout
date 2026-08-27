using IFramework;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityWebSocket;


public class NetSession :IFramework.ServiceBase, IInjectAble
{
    protected override void OnEnter(IServiceCollection services)
    {
        
    }
    protected override void OnQuit(IServiceCollection services)
    {
        Disconnect();
    }
    protected override void OnUse(IServiceCollection services)
    {
        
    }

    private IWebSocket socket;
    public WebSocketState state => socket == null ? WebSocketState.Closed : socket.ReadyState;
    byte[] buffers = new byte[2048];
    byte[] buffers_help = new byte[2048];
    int buffer_len;
    public bool connecting { get; private set; }
    public bool IsConnected => state == UnityWebSocket.WebSocketState.Open;
    public string address { get; private set; }

    public NetSession(string address)
    {
        this.address = address;
    }

    private CancellationTokenSource TokenSource;
    [Inject] IGameStateService stateService;
    static MethodInfo secondNotify;
    static Dictionary<Type, MethodInfo> map = new Dictionary<Type, MethodInfo>();
    static MethodInfo GetMethod(Type type)
    {
        secondNotify = secondNotify ?? typeof(Events).GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Where(m => m.Name == "Notify" &&
                               m.GetParameters().Length == 2 &&
                               m.GetParameters()[0].ParameterType == typeof(string) &&
                               m.IsGenericMethodDefinition)
                   .FirstOrDefault();
        if (!map.TryGetValue(type, out var result))
        {
            result = secondNotify.MakeGenericMethod(type);
            map[type] = result;
        }
        return result;
    }

    private void Decode()
    {
        while (true)
        {
            var result = MessageHelper.Unpack(buffers, ref buffer_len, ref buffers_help);
            if (result == null) break;
            var succ = MessageHelper.FromBytes(result.Array, out var id, out var message);
            if (!succ)
            {
                MessageHelper.FromUInt16(id, out var high, out var low);
                UnityEngine.Debug.LogError($"收到未知的消息 {id} {high} {low}");
            }
            else
            {
                if (!gameState.IsLogIgnoreMessage(message))
                    UnityEngine.Debug.Log($"<color=#00ffff>收到服务器消息 {message.GetType().Name}</color> {ActionBuffer.BuffSerializer.ToJson(message)}");
                if (message is HeartResp heart)
                {
                    Events.Publish(heart);
                }
                else if (message is BaseResp resp)
                {
                    var type = message.GetType();
                    var method = GetMethod(type);
                    if (resp.code != SystemErrorCode.Success)
                    {
                        gameState.ShowResponseErr(resp);
                    }
                    method.Invoke(null, new object[] { type.Name, resp });
                }
                else
                {
                    Events.Publish(message.GetType().Name, message as IPush);

                }
            }
        }

    }
    private void Socket_OnOpen(object sender, OpenEventArgs e)
    {
        //Game.BindUpdate(Decode);
        OnEndConnect(true);
    }

    private void Socket_OnClose(object sender, CloseEventArgs e)
    {
        //Game.UnBindUpdate(Decode);
        Debug.LogError($"Socket 关闭了 cede {e.Code}StatusCode {e.StatusCode} Reason{e.Reason}WasClean {e.WasClean}");
        OnEndConnect(false);

    }
    private AsyncTask wait;
    private async void Socket_OnMessage(object sender, MessageEventArgs e)
    {
        var bytes = e.RawData;
        buffers = MessageHelper.OnRecMessage(buffers, ref buffer_len, bytes, 0, bytes.Length);
        if (wait != null) return;
        wait = AsyncTask.NextFrame();
        await wait;
        Decode();
        wait = null;
    }
    private AsyncTask<bool> task_con;
    private void SetConResult(bool con)
    {
        //if (!connecting) return;
        if (task_con == null) return;
        task_con.SetResult(con);
        task_con = null;
    }

    private GameState_TryRecon gameState;
    public AsyncTask<bool> Connect()
    {
        gameState = (stateService.FindState<GameState_TryRecon>() as GameState_TryRecon);
        this.address = address;
        task_con = AsyncTask<bool>.CreateFromPool();
        socket = new WebSocket(address);
        socket.ConnectAsync();
        socket.OnMessage += Socket_OnMessage;
        socket.OnClose += Socket_OnClose;
        socket.OnError += (_, e) => Debug.LogError($"Socket 出现错误 {e.Exception} {e.Message}"); ;
        socket.OnOpen += Socket_OnOpen;

        connecting = true;
        TokenSource = new CancellationTokenSource();
        gameState?.OnBeginConnect();
        Log.L("开始链接网络 {0}", this.address);
        return task_con;
    }
    public bool Send(IRequest msg)
    {
        if (!IsConnected) return false;
        var send = MessageHelper.EncodeBytes(msg);
        if (!gameState.IsLogIgnoreMessage(msg))
            Debug.Log($"<color=#00ffff>发消息给服务器 {msg.GetType().Name}</color> {ActionBuffer.BuffSerializer.ToJson(msg)}");
        socket.SendAsync(send);
        return true;
    }
    public async AsyncTask<T> Send<T>(IRequest<T> msg) where T : IResponse
    {
        var succ = Send((IRequest)msg);
        if (succ)
            return await Events.Wait<T>(TokenSource.Token);
        return default;
    }
    public void Disconnect()
    {
        TokenSource?.Cancel();
        TokenSource = null;
        if (socket != null)
        {
            socket.CloseAsync();
            socket.OnMessage -= Socket_OnMessage;
            socket.OnClose -= Socket_OnClose;
            socket.OnOpen -= Socket_OnOpen;
        }
        socket = null;
    }
    private void OnEndConnect(bool succ)
    {
        if (!connecting) return;
        connecting = false;
        gameState?.OnEndConnect(succ);
        SetConResult(succ);
        if (succ) Log.L($"网络链接成功");
        else Log.L($"网络链接失败");
    }


}

