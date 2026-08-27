using Lockstep;
using System;
using System.Collections.Generic;
using static GamePlay.Services;
namespace GamePlay
{
    [Backup]
    public partial class SkillSignalQueue
    {

        private struct Signal
        {
            public LFloat time;
            public SkillSignal signal;
        }
        [Backup]
        public partial class DyValue
        {
            public int index;
            public int value;
        }
        [Backup] public List<long> Hited;
        [Backup] public long sender;
        [Backup] public string player;
        [Backup] public SkillEventType type;
        [Backup] public LFloat startTime;
        [Backup] public int skill_id;
        [Backup] private List<DyValue> dys = new List<DyValue>();
        private Dictionary<int, DyValue> dy_map = new Dictionary<int, DyValue>();

        private List<Queue<Signal>> signals = new List<Queue<Signal>>();
        List<SkillProperty> properties = new List<SkillProperty>();
        private Dictionary<SkillPropertyType, long> values = new();
        private void CalcProperties(PropertyComp property)
        {
            values.Clear();
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var type = prop.type;
                long result = 0;
                values.TryGetValue(type, out result);
                if (prop.effect == ValueEffectType.Fixed)
                {
                    result += prop.value;
                }
                else if (prop.effect == ValueEffectType.Percent)
                {
                    var calc = prop.clac;
                    var p = property.GetProperty(calc);
                    if (p != null)
                    {
                        result += Property.GetByPercent(p.value, prop.percent);
                    }
                }
                values[type] = result;
            }

        }
        public long GetSkillProperty(SkillPropertyType type)
        {
            if (values.TryGetValue(type, out var result))
                return result;
            return 0;
        }
        public void Init()
        {
            GameHelper.SetListToPool(dys);
        }



        private void BuildSignal(Queue<Signal> signals, float _time, SkillSignal signal)
        {
            if (GameContext.state.time > _time.ToLFloat()) return;
            var sig = new Signal();
            sig.time = _time.ToLFloat();
            sig.signal = signal;
            signals.Enqueue(sig);
        }
        private void BuildFor(Queue<Signal> signals, SkillAction parent, SkillForClip @for, ref float _time)
        {
            var count = @for.Count;

            for (int i = 0; i < count; i++)
            {
                var _action = @for.clips[i];
                if (_action is SkillTimeClip time)
                    _time += time.time;
                else if (_action is SkillForClip _for)
                    BuildFor(signals, @for, _for, ref _time);
                else
                    BuildSignal(signals, _time, _action as SkillSignal);
            }
        }
        private Queue<Signal> BuildSeq(SkillClipSequence seq)
        {
            if (seq.conditions != null)
            {
                for (int i = 0; i < seq.conditions.Count; i++)
                {
                    var condition = seq.conditions[i];
                    if (!condition.Execute(this)) return default;
                }
            }
            Queue<Signal> signals = StaticPool.Get<Queue<Signal>>();
            signals.Clear();
            float _time = startTime;
            for (int i = 0; i < seq.clips.Count; i++)
            {
                var _action = seq.clips[i];
                if (_action is SkillTimeClip time)
                    _time += time.time;
                else if (_action is SkillForClip _for)
                    BuildFor(signals, seq, _for, ref _time);
                else
                    BuildSignal(signals, _time, _action as SkillSignal);
            }
            return signals;
        }

        public void Build()
        {
            GameHelper.ListFitDictionary(dys, dy_map, (x) => x.index);
            GameHelper.SetListToPool(signals);
            var modify = GameHelper.FindSkillModify(player, skill_id);
            var asset = helper.GetSkillAsset(skill_id);

            properties.Clear();
            properties.AddRange(asset.property.properties);
            if (modify != null)
            {
                var indexes = modify.GetModifies();
                for (int i = 0; indexes.Count > i; i++)
                {
                    var data = asset.modifies[indexes[i]];
                    properties.AddRange(data.property.properties);
                    if (data.sets != null)
                    {
                        for (int j = 0; j < data.sets.Count; j++)
                        {
                            var set = data.sets[j];
                            set.Execute(this);
                        }
                    }
                }
            }
            CalcProperties(actor.Find(sender).Property);
            List<SkillClipSequence> seqs = asset.GetEvents(type);
            if (seqs != null)
                for (int j = 0; j < seqs.Count; j++)
                {
                    var queue = BuildSeq(seqs[j]);
                    if (queue != null)
                        signals.Add(queue);
                }

        }
        private bool _update(Queue<Signal> signals)
        {
        Again:
            if (signals.Count <= 0) return false;
            var sig = signals.Peek();
            if (sig.time > GameContext.state.time) return true;
            sig.signal.Execute(this);
            signals.Dequeue();
            goto Again;
        }
        public bool Update()
        {
            bool succ = false;
            for (int i = 0; i < signals.Count; i++)
            {
                var queue = signals[i];
                if (queue.Count > 0)
                    succ |= _update(queue);
            }
            return succ;
        }

        public void SetDynamicValue(int index, int value)
        {
            if (dy_map.TryGetValue(index, out var _value))
            {
                _value.value = value;
            }
            else
            {
                var dy = StaticPool.Get<DyValue>();
                dy.value = value;
                dy.index = index;
                dys.Add(dy);
                dy_map[index] = dy;
            }
        }
        public int GetDynamicValue(int index)
        {
            if (dy_map.TryGetValue(index, out var value))
            {
                return value.value;
            }
            return 0;
        }

        public bool ContainsModify(int index)
        {
            var modify = GameHelper.FindSkillModify(player, skill_id);
            if (modify != null)
                return modify.ContainsModify(index);
            return false;
        }
    }
}


