using Lockstep;
using System.Collections.Generic;
namespace GamePlay
{
    [Backup]
    public abstract partial class PropertyComp : Component<Actor>
    {
        private Dictionary<PropertyType, Property> properties = new Dictionary<PropertyType, Property>();
        public Property GetProperty(PropertyType type) => properties.TryGetValue(type, out var property) ? property : null;
        protected Property Create(PropertyType type, long value = 0)
        {
            var p = StaticPool.Get<Property>();
            p.Init(type, value);
            properties[type] = p;
            return p;
        }
        protected override void OnReset()
        {
            foreach (var item in properties.Values)
            {
                StaticPool.Set(item);
            }
            properties.Clear();
        }

        public PropertyChangeScope BeginPropChange() => new PropertyChangeScope(this);

        public bool Cost(List<PropertyCost> costs)
        {
            if (costs == null || costs.Count == 0) return true;

            var ps = StaticPool.Get<List<Property>>();
            var vs = StaticPool.Get<List<long>>();
            ps.Clear();
            vs.Clear();
            bool succ = true;
            for (int i = 0; i < costs.Count; i++)
            {
                var cost = costs[i];
                var costProperty = GetProperty(cost.costProperty);
                if (costProperty == null || !costProperty.isFixed)
                {
                    succ = false;
                    break;
                }
                long need = 0;
                if (cost.costType == ValueEffectType.Fixed)
                    need = cost.value;
                else if (cost.costType == ValueEffectType.Percent)
                {
                    var calcProperty = GetProperty(cost.calcProperty);
                    if (calcProperty == null)
                    {
                        succ = false;
                        break;
                    }
                    need = Property.GetByPercent(calcProperty.value, cost.floatValue);
                }
                if (need > costProperty.value)
                {
                    succ = false;
                    break;
                }
                var index = ps.IndexOf(costProperty);
                if (index >= 0)
                {
                    if (need > costProperty.value + vs[index])
                    {
                        succ = false;
                        break;
                    }
                    vs[index] -= need;
                }
                else
                {
                    ps.Add(costProperty);
                    vs.Add(-need);
                }
            }


            if (succ)
                using (var scope = BeginPropChange())
                    for (int i = 0; i < ps.Count; i++)
                        scope.PushFixedProp(ps[i].type, vs[i]);


            StaticPool.Set(ps);
            StaticPool.Set(vs);
            return succ;
        }
    }

    [Backup]
    public abstract partial class PropertyComp<T> : PropertyComp where T : class
    {
        public void ReadProperty(T data)
        {
            OnSyncData(data);
        }
        protected abstract void OnSyncData(T data);
    }
}
