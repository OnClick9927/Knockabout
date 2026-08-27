using Lockstep;
using System.Collections.Generic;
namespace GamePlay
{
    [Backup]
    public partial class AbilityComp : Component<Actor>,IUpdate
    {

        [Backup]
        public partial class Entity
        {

            [Backup] public int id;
            public bool needUpdate => cfg.Type == Ability.AbilityType.Update;
            [System.NonSerialized] public Ability cfg;
            [Backup] public LFloat invokeTime;
            public void CalcInvokeTime()
            {
                invokeTime = GameContext.state.time + cfg.cd.ToLFloat();
            }
   

            internal bool CouldUpdateInvoke()
            {
                if (!needUpdate) return false;
                return invokeTime <= GameContext.state.time;
            }
        }


        [Backup] private List<Entity> abilities = new();
        private Dictionary<int, Entity> ability_map = new();
        protected override void OnReset()
        {
            GameHelper.SetListToPool(abilities);
            ability_map.Clear();
        }
        protected override void OnAwake()
        {     
            update_context = AbilityTriggerParam.Update(this.target);
        }
        protected override void OnEndReadBackUp()
        {
            update_context = AbilityTriggerParam.Update(this.target);
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                ability.cfg = Services.helper.LoadAbility(ability.id);
                ability_map[ability.id] = ability;
            }
        }

        public bool AddAbility(int ability_id)
        {
            if (ability_map.TryGetValue(ability_id, out var ability))
                return false;
            Entity a = StaticPool.Get<Entity>();
            a.id = ability_id;
            a.cfg = Services.helper.LoadAbility(ability_id);
            a.CalcInvokeTime();
            a.cfg.OnAdd(this);
            abilities.Add(a);
            ability_map[ability_id] = a;
            return true;
        }

        public bool RemoveAbility(int ability_id)
        {
            if (!ability_map.TryGetValue(ability_id, out var ability)) return false;
            for (int i = 0; i < abilities.Count; i++)
            {
                var _ability = abilities[i];
                if (_ability.id == ability_id)
                {
                    abilities.RemoveAt(i);
                    break;
                }
            }
            ability_map.Remove(ability_id);
            ability.cfg.OnRemove(this);
            StaticPool.Set(ability);
            return true;
        }
        private AbilityTriggerParam update_context;
        void IUpdate.Update()
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                if (!ability.CouldUpdateInvoke()) continue;
                ability.cfg.TriggerEffect(this, update_context);
                ability.CalcInvokeTime();
            }
        }



        public void Trigger(AbilityTriggerParam trigger)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                ability.cfg.TriggerEffect(this, trigger);
            }
        }


    }
}


