using Lockstep;
using System.Collections.Generic;

namespace GamePlay
{
    [Backup]
    public partial class Property
    {
        [Backup]
        public partial class Layer
        {
            [Backup] public long value;
            [Backup] public long percentValue;
            [Backup] public PropertyLayer layer;
        }
        public Layer GetLayer(PropertyLayer layer)
        {
            int index = 0;
            if (layers.Count > 0)
                for (var i = 0; i < layers.Count; i++)
                {
                    var prop = layers[i];
                    if (prop.layer == layer) return prop;
                    if (prop.layer > layer)
                    {
                        index = i;
                        break;
                    }
                }
            var _bonus = StaticPool.Get<Layer>();
            _bonus.layer = layer;
            _bonus.value = _bonus.percentValue = 0;
            layers.Insert(index, _bonus);
            return _bonus;
        }
        public bool CalcProp(out long src)
        {
            long result = 0;
            for (var i = 0; i < layers.Count; i++)
            {
                long percent = layers[i].percentValue;
                long add = layers[i].value;
                result += add;
                result += GetByPercent(result, percent);
            }
            src = this.value;
            if (src == result) return false;
            this.value = result;
            return true;
        }
        private void _PushProp(PropertyLayer layer,
          long value, bool IsPercent)
        {

            if (isFixed)
                layer = PropertyLayer.Base;
            var _layer = GetLayer(layer);
            if (IsPercent)
                _layer.percentValue += value;
            else
                _layer.value += value;
        }
        public void PushProp(PropertyLayer layer, long value) => _PushProp(layer, value, false);
        public void PushPropPercent(PropertyLayer layer, float value) => _PushProp(layer, (long)(value * Property.PercentMul), true);

        public static long GetByPercent(long src, float percent) => (long)(src * percent);
        public static long GetByPercent(long src, long percent) => GetByPercent(src, (float)percent / PercentMul);

        const int PercentMul = 10000;




        [Backup] public List<Layer> layers = new List<Layer>();
        [Backup] public long value { get; private set; }

        public bool isFixed { get; private set; }




        public static bool IsFixedProperty(PropertyType property)
        {
            if (property == PropertyType.HP || property == PropertyType.Coin)
                return true;
            return false;
        }
        public PropertyType type { get; private set; }
        public void Init(PropertyType type, long value)
        {
            isFixed = Property.IsFixedProperty(type);
            this.value = value;
            this.type = type;
        }
        public static implicit operator long(Property type)
        {
            return type.value;
        }
    }

}
