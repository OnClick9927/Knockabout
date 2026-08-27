/*********************************************************************************
 *Author:         OnClick
 *Version:        0.1
 *UnityVersion:   2021.3.33f1c1
 *Date:           2024-04-25
*********************************************************************************/
using Lockstep;
using System.Collections.Generic;
using static GamePlay.BuffAsset.Buff;
namespace GamePlay
{
    [Backup]
    public partial class BuffComp : Component<Actor>, IUpdate
    {
        [Backup] private long uid = 1;
        [Backup] private List<Buff> buffs = new List<Buff>();
        private Dictionary<long, Buff> buff_map = new Dictionary<long, Buff>();
        private Dictionary<int, List<Buff>> id_uid = new Dictionary<int, List<Buff>>();
        public IReadOnlyList<Buff> GetBuffList() => buffs;
        private List<Buff> FindBuffs(int cfg_id)
        {
            return id_uid.TryGetValue(cfg_id, out var buff) ? buff : default;
        }

        protected override void OnAwake()
        {

        }
        protected override void OnReset()
        {
            base.OnReset();
            GameHelper.SetListToPool(buffs);
            foreach (var item in id_uid.Values)
                StaticPool.Set(item);

            uid = 1;
            buff_map.Clear();
            id_uid.Clear();
        }


        void IUpdate.Update()
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                buff.Update();
                if (buff.NeedRemoveByTimeNow())
                    RemoveBuff(buff);
            }
        }


        private Buff Create(BuffAsset.Buff data, Actor sender)
        {
            var buff = StaticPool.Get<Buff>();
            buff.uid = ++uid;
            buff.cfg_id = data.Id;
            buff.target = target;
            buff.sender = -1;
            if (sender != null)
                buff.sender = sender.uid;
            buff.actor_sender = sender;

            buff.layer = 1;
            buff.actor = this.actor;
            buff.ReadCfg();
            return buff;
        }
        private Buff _AddBuff(Buff buff, bool back)
        {
            if (!back)
                buffs.Add(buff);
            buff_map[buff.uid] = buff;
            var cfg_id = buff.cfg_id;
            if (!id_uid.TryGetValue(cfg_id, out var result))
            {
                result = StaticPool.Get<List<Buff>>();
                result.Clear();
                id_uid[cfg_id] = result;
            }
            result.Add(buff);
            if (!back)
                buff.OnAdd();
            return buff;
        }
        private bool RemoveBuff(Buff buff)
        {
            var cfg_id = buff.cfg_id;
            if (!buff_map.Remove(buff.uid)) return false;
            if (!id_uid.TryGetValue(cfg_id, out var result)) return false;
            result.Remove(buff);
            buffs.Remove(buff);
            buff.OnRemove();
            if (result.Count == 0)
            {
                id_uid.Remove(cfg_id);
                StaticPool.Set(result);
            }
            return true;
        }
        public long AddBuff(int id, Actor sender)
        {

            BuffAsset.Buff data = Services.helper.LoadBuff(id);
            var tags = actor.tags;
            if (tags.ContainsAnyTag(data.noTags))
                return -1;
            if (!tags.ContainsAllTag(data.needTags))
                return -1;
            switch (data.addType)
            {
                case AddType.Immediately:
                    {
                        var buff = Create(data, sender);
                        buff.OnAdd();
                        StaticPool.Set(buff);
                        return 0;
                    }
                case AddType.Single:
                    return _AddBuff(Create(data, sender), false).uid;
                case AddType.Replace://不可能有多个
                    {
                        var find = FindBuffs(id);
                        if (find != null && find.Count == 1)
                            RemoveBuff(find[0]);
                        return _AddBuff(Create(data, sender), false).uid;
                    }
                case AddType.Layers://不可能有多个
                    {
                        var find = FindBuffs(id);
                        if (find != null && find.Count == 1)
                        {
                            find[0].OnAddLayer();
                            find[0].actor_sender = sender;
                            find[0].sender = -1;
                            if (sender != null)
                                find[0].sender = sender.uid;
                            return find[0].uid;
                        }
                        else
                            return _AddBuff(Create(data, sender), false).uid;
                    }
            }
            return -1;
        }
        public bool RemoveBuff(long buff_id)
        {
            var succ = buff_map.TryGetValue(buff_id, out var buff);
            if (succ)
                RemoveBuff(buff);
            return succ;
        }
        public bool RemoveBuffByConfig(int buff_id)
        {
            if (!id_uid.TryGetValue(buff_id, out var list)) return false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                RemoveBuff(list[i]);
            }
            return true;
        }


        protected override void OnEndReadBackUp()
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                buff.actor = Services.actor.Find(buff.target);
                buff.actor_sender = Services.actor.Find(buff.sender);

                buff.ReadCfg();
                _AddBuff(buff, true);
            }
        }

    }
}


