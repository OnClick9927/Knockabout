using System.Collections.Generic;
using ActionBuffer;
namespace GamePlay
{

    public partial class BuffAsset
    {
        public const string path = "Assets/Art/GamePlay/BuffAsset.txt";


        public Dictionary<int, Buff> buffs = new();

        public byte[] ToBytes() => BuffSerializer.ToBytes(this);
        public static BuffAsset FromBytes(byte[] buffer)
        {
            var asset = BuffSerializer.FromBytes(buffer, typeof(BuffAsset)) as BuffAsset;
            instance = asset;
            return asset;
        }
        public static BuffAsset instance;
    }

}


