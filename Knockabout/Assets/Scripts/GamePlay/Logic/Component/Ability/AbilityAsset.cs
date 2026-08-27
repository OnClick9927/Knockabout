using System.Collections.Generic;
using ActionBuffer;
namespace GamePlay
{

    public class AbilityAsset
    {
        public const string path = "Assets/Art/GamePlay/AbilityAsset.txt";

        public Dictionary<int,Ability> abilitys = new Dictionary<int,Ability>();

        public byte[] ToBytes() => BuffSerializer.ToBytes(this);
        public static AbilityAsset FromBytes(byte[] buffer)
        {
            var asset = BuffSerializer.FromBytes(buffer, typeof(AbilityAsset)) as AbilityAsset;
            instance = asset;
            return asset;
        }
        public static AbilityAsset instance;
    }
}


