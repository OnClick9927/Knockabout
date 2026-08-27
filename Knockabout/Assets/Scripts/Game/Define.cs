using IFramework;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using GamePlay;
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
    public struct GameStartArgs:IEventArgs { }

    public struct RecPlayerInputsArgs : IEventArgs {
       public List<PlayerInput> inputs;

        public RecPlayerInputsArgs(List<PlayerInput> inputs)
        {
            this.inputs = inputs;
        }
    }

    public struct SyncHandCardFastArg : IEventArgs
    {
        public IReadOnlyList<int> cards;


        public SyncHandCardFastArg(List<int> cards) 
        {
            this.cards = cards;
        }
    }
    public struct AddCardArg : IEventArgs
    {
        public int card;
        public Vector3 pos;

        public AddCardArg(int card, Vector3 pos)
        {
            this.card = card;
            this.pos = pos;
        }
    }
    public struct UseCardArg : IEventArgs
    {
        public int card_index;
        public int card_id;

        public UseCardArg(int card_index, int card_id)
        {
            this.card_index = card_index;
            this.card_id = card_id;
        }
    }











}
