using System.Collections.Generic;

namespace Luban
{

    public static class Configs
    {
# if UNITY_5_3_OR_NEWER
        public const string Directory = "Assets/Art/Configs";
        public static string GetConfigFile(string file) => $"{Directory}/{file}.bytes";
#endif


        private static Tables tabs;
        private static Dictionary<int, int> role2card = new Dictionary<int, int>();

        public static void Init(Tables tabs)
        {
            Configs.tabs = tabs;
            role2card.Clear();
            foreach (var card in tabs.TbCardData.DataList)
            {
                var role = card.GetRole();
                role2card[role.Id] = card.Id;

            }
        }

        public static GlobalData GetGlobal() => tabs.TbGlobalData.Data;
        public static TbSoundData GetSound() => tabs.TbSoundData;
        public static LevelData GetLev(int id) => tabs.TbLevelData.GetOrDefault(id);
        public static PlayerProperty GetPlayerProperty(int id) => tabs.TbPlayerProperty.GetOrDefault(id);
        public static Role GetRole(int id) => tabs.TbRole.GetOrDefault(id);
        private static RoleLev GetRoleLev(int id, int lev) => tabs.TbRoleLev.Get(id, lev);
        public static CardData GetCard(int id) => tabs.TbCardData.GetOrDefault(id);
        public static ItemData GetItem(int id) => tabs.TbItemData.GetOrDefault(id);


        public static Role Role(this RoleLev role, int lev = -1) => GetRole(role.Id);
        public static RoleLev LevConfig(this Role role, int lev = -1)
        {
            if (lev == -1)
                lev = role.DefaultLev;
            return GetRoleLev(role.Id, lev);
        }

        public static Role GetRole(this CardData card) => GetRole(card.Role);
        public static CardData GetCard(this Role role)
        {
            if (!role2card.TryGetValue(role.Id, out var card_id)) return null;
            return GetCard(card_id);
        }



    }

}

