using ActionEditor;
using ActionAttribute;
using System;
namespace GamePlay
{
    [Serializable]
    public abstract class BuffEffect
    {
        public enum TriggerType
        {
            Jump,//Ò»Φ±Εά
            Add,
            Remove
        }
        [ReadOnly]public TriggerType trigger;
        public abstract void DoEffect(Buff buff);

       
    }
}


