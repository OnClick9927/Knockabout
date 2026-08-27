using GamePlay;
using IFramework;
using Luban;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WooAsset;

public class GameAssets : MonoBehaviour
{
    public AssetReference<GameObject> house = new AssetReference<GameObject>();
    //public AssetReference<GameObject> role = new AssetReference<GameObject>();

    public AssetReference<GameObject> healthBar = new AssetReference<GameObject>();

    public AssetReference<TextAsset> ability = new AssetReference<TextAsset>();
    public AssetReference<TextAsset> actorModify = new AssetReference<TextAsset>();
    public AssetReference<TextAsset> buff = new AssetReference<TextAsset>();

    AssetsGroupOperation op;
    public Asset FindAsset(string path)
    {
        return op.FindAsset(path) as Asset;
    }
    public void Release()
    {
        if (op == null) return;
        op.Release();
        op = null;
    }
    private void Add(string path, HashSet<string> set)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (set.Contains(path)) return;
        set.Add(path);
    }
    public async AsyncTask<AssetsGroupOperation> Prepare()
    {
        var list = StaticPool.Get<HashSet<string>>();
        list.Clear();
        Add(house.path,list);
        Add(healthBar.path, list);
        Add(ability.path, list);
        Add(actorModify.path, list);
        Add(buff.path, list);
        //list.Add(role.path);
        for (int i = 0; i < GameContext.gameData.players.Count; i++)
        {
            var player = GameContext.gameData.players[i]; ;
            foreach (var item in player.roles)
            {
                var id = item.id;
                var lev = item.level;
                var roleCfg = Configs.GetRole(id);
                var roleLevCfg = roleCfg.LevConfig(lev);
                Add(roleCfg.BT, list);
                Add(roleCfg.Prefab, list);
                Add(roleLevCfg.BT, list);
            }

        }


        op = await Assets.PrepareAssets(list.ToList());
        StaticPool.Set(list);
        return op;
    }
}

