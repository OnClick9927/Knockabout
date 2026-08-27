// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

namespace Lockstep
{
    /// <summary>
    /// 跨运行环境的最小调试适配层。
    /// Unity 中转发到 UnityEngine.Debug，脱离 Unity 编译时回退到 System 调试输出。
    /// </summary>
    public class Debug
    {
        public static void Assert(bool succ,string msg)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Assert(succ, msg);
#else
            System.Diagnostics.Debug.Assert(succ, msg);
#endif
        }

        public static void LogError(object message)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError(message);
#else
            System.Console.Error.WriteLine(message);
#endif
        }
    }
}

