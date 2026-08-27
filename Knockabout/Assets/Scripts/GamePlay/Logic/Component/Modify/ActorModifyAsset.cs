using System.Collections.Generic;
using ActionBuffer;
using ActionEditor;
using ActionAttribute;
namespace GamePlay
{
    public partial class ActorModifyAsset
    {
        public const string path = "Assets/Art/GamePlay/ActorModifyAsset.txt";
        [System.Serializable]
        public class Modify
        {
            public List<ActorModifyEffect> Effects = new List<ActorModifyEffect>();
            [ReadOnly] public int Id;
            public string Name;

            public ActorType actorType;
            [Condition(ConditionMode.Show, nameof(actorType), ActorType.Role)]
            public int role_cfg_id;

        }

        public Dictionary<int,Modify> buffs = new();

        public byte[] ToBytes() => BuffSerializer.ToBytes(this);
        public static ActorModifyAsset FromBytes(byte[] buffer)
        {
            var asset = BuffSerializer.FromBytes(buffer, typeof(ActorModifyAsset)) as ActorModifyAsset;
            instance = asset;
            return asset;
        }
        public static ActorModifyAsset instance;
    }
}


