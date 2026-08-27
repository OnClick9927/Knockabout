
namespace Proto
{
    [MessageCode(MessageCode.Sys, 1)]
    public class HeartReq : IRequest
    {

    }
    [MessageCode(MessageCode.Sys, 2)]
    public class HeartResp : BaseResp
    {
    }


    [MessageCode(MessageCode.Sys, 4)]
    public class KickOutPush : IPush
    {
        public enum Reason
        {
            None,
            DuplicateLogins,
        }
        public Reason reason;
    }
}
