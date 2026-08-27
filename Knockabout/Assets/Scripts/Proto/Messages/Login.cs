
namespace Proto
{
    [MessageCode(MessageCode.Account, 1)]

    public class SignInReq : IRequest<SignInResp>
    {
        public string account;
        public string password;
    }
    [MessageCode(MessageCode.Account, 2)]
    public class SignInResp : BaseResp
    {
        public class Err : SystemErrorCode
        {
            public static int ExistAccount = 10000;
        }
    }



    [MessageCode(MessageCode.Account, 3)]
    public class LoginReq : IRequest<LoginResp>
    {
        public string account;
        public string password;
    }
    [MessageCode(MessageCode.Account, 4)]
    public class LoginResp : BaseResp
    {
        public class Err:SystemErrorCode
        {
            public static int AccountNotExist = 10000;
            public static int PasswordErr = 10001;
        }
        public string uid;
        public string name;
        public long serverTime;
    }
}
