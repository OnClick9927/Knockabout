using System;
namespace Proto
{
    public interface IMessage { }
    public interface IRequest : IMessage { }
    public interface IRequest<TResponse> : IRequest where TResponse : IResponse { }
    public interface IResponse : IMessage
#if UNITY_5_3_OR_NEWER
        , IFramework.IEventArgs
#endif
    {

    }
    public class BaseResp : IResponse
    {
        public int code;
    }

    public interface IPush : IMessage
#if UNITY_5_3_OR_NEWER
        , IFramework.IEventArgs
#endif

    { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MessageCodeAttribute : System.Attribute
    {
        public byte main { get; private set; }
        public byte sub { get; private set; }

        public MessageCodeAttribute(byte main, byte sub)
        {
            this.main = main;
            this.sub = sub;
        }
    }



}
