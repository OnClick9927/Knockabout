using System;
using UnityEngine;
namespace AOT
{
    [CreateAssetMenu]
    public class AOTDefine : ScriptableObject
    {
        [Flags]
        public enum LogType
        {
            Log = 2,
            Warn = 4,
            Err = 8,
        }
        public LogType logType;

        public enum ServerChannel
        {
            // Service = -1,
            Dev = 0,
            TapTap = 1,
        }
        public enum ClientChannel
        {
            Dev = 0,
            Formal = 1,
            TapTap = 2,
        }

        public static AOTDefine G;

        //public bool PrefCompress = false;
        //public string PrefId = "15";
        public string LocalizationType = "zh-cn";
        public bool LocalTestMode = true;
        //public bool ShowHearBeat = false;


        //public ServerChannel channel
        //{
        //    get
        //    {
        //        if (clientChannel == ClientChannel.Dev)

        //            return ServerChannel.Dev;
        //        return ServerChannel.TapTap;
        //    }


        //}
        //public ClientChannel clientChannel = ClientChannel.Dev;

        public string GateUrl = "http://49.235.171.165/center";


        public const string HotAssemblyTag = "HotAssembly";
        public const string ConfigAssetTag = "Config";
        public const string ASBDir = "Assets/Project/HotAssembly";
        //public static string GGamePrefab = "Assets/Project/Prefabs/GGame.prefab";
    }
}
