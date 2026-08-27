using Lockstep;
using System.Collections.Generic;
using static GamePlay.Services;
namespace GamePlay
{
    [Backup]
    public partial class SkillComp : Component<Actor>, IUpdate
    {
        [Backup]
        public partial class Modify
        {
            [Backup] public int Skill_id;
            [Backup] private List<int> indexes = new List<int>();
            internal bool ContainsModify(int index) => indexes.Contains(index);
            public void ModifyByName(string name)
            {
                var asset = Services.helper.GetSkillAsset(Skill_id);
                for (int i = 0; i < asset.modifies.Count; i++)
                {
                    if (asset.modifies[i].rgName == name)
                    {
                        ModifyByIndex(i);
                        break;
                    }
                }
            }
            public void ModifyByIndex(int index)
            {
                var asset = helper.GetSkillAsset(Skill_id);
                var modify = asset.modifies[index];
                if (indexes.Contains(index))
                {
                    helper.Error($"same modify {Skill_id} {index}");
                    return;
                }
                var _index = -1;

                for (int i = 0; i < indexes.Count; i++)
                {
                    if (indexes[i] > index)
                    {
                        _index = i;
                        break;
                    }
                }
                if (_index == -1)
                    indexes.Add(index);
                else
                    indexes.Insert(_index, index);
            }
            public IReadOnlyList<int> GetModifies() => indexes;
        }

        [Backup]
        public partial class Skill_CD
        {
            [Backup] public int skill_id;
            [Backup] public LFloat End;
            [Backup] public bool waitCDBegain;
            [Backup] public LFloat CD;
            public void CalcEnd()
            {
                End = GameContext.state.time + CD;
            }
        }
        [Backup] private List<SkillSignalQueue> queues = new List<SkillSignalQueue>();
        [Backup] private List<Skill_CD> cds = new List<Skill_CD>();
        [Backup] private List<Modify> modifies = new();

        private Dictionary<int, Skill_CD> skill_cd_map = new Dictionary<int, Skill_CD>();
        private Dictionary<int, Modify> map = new();
        protected override void OnAwake()
        {

        }
        protected override void OnReset()
        {
            GameHelper.SetListToPool(queues);
            GameHelper.SetListToPool(cds);
            modifies.Clear();
            map.Clear();
            skill_cd_map.Clear();
        }
        protected override void OnEndReadBackUp()
        {
            for (int i = 0; i < queues.Count; i++)
                queues[i].Build();
            GameHelper.ListFitDictionary(cds, skill_cd_map, (x) => x.skill_id);
            GameHelper.ListFitDictionary(modifies, map, (x) => x.Skill_id);
        }

        void IUpdate.Update()
        {
            for (int i = cds.Count - 1; i >= 0; i--)
            {
                var skill = cds[i];
                if (skill.waitCDBegain) continue;
                if (skill.End <= GameContext.state.time)
                {
                    StaticPool.Set(skill);
                    cds.RemoveAt(i);
                    skill_cd_map.Remove(skill.skill_id);
                }
            }


            for (int i = queues.Count - 1; i >= 0; i--)
                if (!queues[i].Update())
                {
                    StaticPool.Set(queues[i]);
                    queues.RemoveAt(i);
                }
        }

        private SkillSignalQueue PlaySkillAction(int skill_id, SkillEventType type)
        {
            SkillSignalQueue queue = StaticPool.Get<SkillSignalQueue>();
            queue.sender = this.target;
            queue.player = this.player;
            queue.type = type;
            queue.startTime = GameContext.state.time;
            queue.skill_id = skill_id;
            queue.Hited?.Clear();
            queue.Init();
            queue.Build();
            return queue;
        }
        private void Play(SkillSignalQueue queue) =>
                queues.Add(queue);

        public bool PlaySkill(int skill_id, List<long> hited)
        {
            if (skill_cd_map.TryGetValue(skill_id, out var skill_CD)) return false;

            var comp_tags = actor.tags; 
            SkillAsset asset = helper.GetSkillAsset(skill_id);
            var tags= actor.tags;
            if (tags.ContainsAnyTag(asset.noTags))
                return false;
            if (!tags.ContainsAllTag(asset.needTags))
                return false;

           
            if (!actor.Property.Cost(asset.costs))
                return false;

            var queue = PlaySkillAction(skill_id, SkillEventType.Begin);
            queue.Hited = hited;
            var cd_type = asset.cdType;
            if (cd_type == SkillAsset.CDType.WaitSkill || cd_type == SkillAsset.CDType.Normal)
            {
                var skill = StaticPool.Get<Skill_CD>();
                skill.skill_id = skill_id;
                skill.waitCDBegain = cd_type == SkillAsset.CDType.WaitSkill;
                skill.CD = asset.cd.ToLFloat();
                if (!skill.waitCDBegain)
                    skill.CalcEnd();
                cds.Add(skill);
                skill_cd_map.Add(skill_id, skill);
            }
            Play(queue);
            return true;
        }
        private void SkillEnterCD(int skill_id)
        {
            if (!skill_cd_map.TryGetValue(skill_id, out var skill_CD)) return;
            if (!skill_CD.waitCDBegain) return;
            skill_CD.waitCDBegain = false;
            skill_CD.CalcEnd();
        }



        public void AbortSkill(int skill_id)
        {
            for (int i = 0; i < queues.Count; i++)
            {
                var eve = queues[i];
                if (eve.skill_id == skill_id && eve.type == SkillEventType.Begin)
                {
                    SkillEnterCD(eve.skill_id);
                    StaticPool.Set(queues[i]);
                    queues.RemoveAt(i);
                    break;
                }
            }

        }

        protected override void OnEvent(IActorEvent eve)
        {
            base.OnEvent(eve);
            switch (eve)
            {
                case OnSkillEndEvent end:
                    {
                        var queue = PlaySkillAction(end.skill_id, SkillEventType.End);
                        Play(queue);
                        break;
                    }
                case OnSkillHitEvent hit:
                    {
                        var queue = PlaySkillAction(hit.skill_id, SkillEventType.Hit);
                        queue.Hited = hit.hit;
                        Play(queue);
                        break;
                    }
                case SkillEnterCDEvent cd:
                    {
                        SkillEnterCD(cd.skill_id);
                        break;
                    }
                case PlaySkillEvent play:
                    {
                        PlaySkill(play.skill_id, play.hit);
                        break;
                    }
                case AbortSkillEvent abort:
                    {
                        AbortSkill(abort.skill_id);
                        break;
                    }
                default:
                    break;
            }
        }


        public Modify FindModify(int skill) => map.TryGetValue(skill, out var result) ? result : null;
        public void ModifyByName(int skill, string rgName)
        {
            if (!map.TryGetValue(skill, out var context))
            {
                context = new()
                {
                    Skill_id = skill,
                    //player = player,
                };
                modifies.Add(context);
                map.Add(skill, context);
            }

            context.ModifyByName(rgName);
        }
        public void ModifyByIndex(int skill, int rgIndex)
        {
            if (!map.TryGetValue(skill, out var context))
            {
                context = new()
                {
                    Skill_id = skill,
                    //player = player,
                };
                modifies.Add(context);
                map.Add(skill, context);
            }
            context.ModifyByIndex(rgIndex);
        }
    }
}


