using IFramework;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
public class ResDefine
{
    public static string loginScene = "Assets/Art/Game/Scenes/Login.unity";
    public static string mainScene = "Assets/Art/Game/Scenes/Main.unity";

}

public partial class EventDefine
{
    public struct LoadSceneArgs : IEventArgs
    {
        public readonly string sceneName;
        public readonly LoadSceneMode mode;

        public LoadSceneArgs(string sceneName, LoadSceneMode mode)
        {
            this.sceneName = sceneName;
            this.mode = mode;
        }
        //public Action complete;
    }
    public struct LoadSceneEndArgs : IEventArgs { }
    public struct ShowTipArgs : IEventArgs
    {
        public readonly string tip;

        public ShowTipArgs(string tip)
        {
            this.tip = tip;
        }
    }



    public struct UITopVisibleChange : IEventArgs
    {
        public readonly string path;
        public readonly int layer;

        public UITopVisibleChange(string path, int layer)
        {
            this.path = path;
            this.layer = layer;
        }
    }















}
