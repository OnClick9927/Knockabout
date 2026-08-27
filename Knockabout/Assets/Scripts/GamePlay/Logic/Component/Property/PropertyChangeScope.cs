using Lockstep;
using System;
using System.Collections.Generic;
namespace GamePlay
{
    public struct PropertyChangeScope : IDisposable
    {
        private PropertyComp comp;
        private HashSet<Property> ps;
        public PropertyChangeScope(PropertyComp comp)
        {
            this.comp = comp;
            this.ps = StaticPool.Get<HashSet<Property>>();
            ps.Clear();
        }

        public void PushFixedProp(PropertyType property, long value)
         => PushProp(PropertyLayer.Base, property, value);
        public void PushProp(PropertyLayer layer, PropertyType property, long value)
        {
            var p = comp.GetProperty(property);
            p.PushProp(layer, value);
            ps.Add(p);
        }
        public void PushPropPercent(PropertyLayer layer, PropertyType property, float value)
        {
            var p = comp.GetProperty(property);
            p.PushPropPercent(layer, value);
            ps.Add(p);
        }

        public void Dispose()
        {
            foreach (var p in ps)
                if (p.CalcProp(out var src))
                    GameHelper.DoActorEvent(comp.actor, new OnPropertyChangedEvent(p.type, src, p.value));
            ps.Clear();
            StaticPool.Set(ps);
        }
    }
}
