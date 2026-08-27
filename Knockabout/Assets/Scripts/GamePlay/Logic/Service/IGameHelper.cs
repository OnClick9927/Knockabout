using Lockstep;
using System;
using System.Collections.Generic;
using static GamePlay.AbilityEffect;
using static UnityEngine.GraphicsBuffer;

namespace GamePlay
{
    public partial interface GameHelper : IService
    {
        public void Error(string msg);
        public void Log(string msg);
        void SendPlayerInputToServer();
        ActorBTAsset LoadRoleBTAsset(Luban.Role roleCfg, Luban.RoleLev roleLevCfg);

        SkillAsset GetSkillAsset(int skill);
        BuffAsset.Buff LoadBuff(int id);
        Ability LoadAbility(int ability_id);
        ActorModifyAsset.Modify Load(int id);


        public static Dictionary<K, V> ListFitDictionary<K, V>(List<V> list, Dictionary<K, V> map
            , Func<V, K> func)
        {
            map = map ?? new Dictionary<K, V>();
            for (int i = 0; i < list.Count; i++)
            {
                var v
                    = list[i];
                map.Add(func(v), v);
            }
            map.Clear();
            return map;
        }

        public static void SetListToPool<T>(List<T> list) where T : class
        {
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                StaticPool.SetByRealType(value);
            }
            list.Clear();
        }
        public static void DoActorEvent(Actor actor, IActorEvent eve)
        {
            if (eve is not IActorEvent_JustView)
            {
                actor.ExecuteEvent(eve);
            }

            Services.view?.OnActorEvent(actor, eve);
            if (eve is IActorEvent_After after)
                after.AfterExecute(actor);
        }

        public static List<T> Shuffle<T>(List<T> list)
        {
            if (list == null) return list;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = GameContext.state.random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
            return list;
        }
        public static T[] Shuffle<T>(T[] array)
        {
            if (array == null) return array;
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = GameContext.state.random.Next(n + 1);
                (array[k], array[n]) = (array[n], array[k]);
            }
            return array;
        }




        public delegate bool DoTargetCall<Value, TComponent>(Actor sender, TComponent comp, Value value);
        static bool DoTarget<Value, TComponent>(TargetType type, Value value, Actor sender, Actor target,
      DoTargetCall<Value, TComponent> action) where TComponent : Component
        {
            TComponent comp = null;
            PlayerActor player = null;
            if (type == TargetType.Self)
                comp = sender.FindComponent<TComponent>();
            else if (type == TargetType.Target)
                comp = target.FindComponent<TComponent>();
            else if (type == TargetType.Player)
                player = sender.player;
            else if (type == TargetType.TargetPlayer)
                player = target.player;
            if (player != null)
                comp = player.FindComponent<TComponent>();
            if (comp == null) return false;
            return action(sender, comp, value);
        }

        static bool DoTarget<Value, TComponent>(TargetType type, Value value, long sender, long target,
        DoTargetCall<Value, TComponent> action) where TComponent : Component
        {
            Actor _target = null;
            if (type == TargetType.Target || type == TargetType.TargetPlayer)
                _target = Services.actor.Find(target);
            return DoTarget(type, value, Services.actor.Find(sender), _target, action);
        }
        static bool DoTarget<Value, TComponent>(TargetType type, Value value,
      long sender, List<long> targets,
        DoTargetCall<Value, TComponent> action) where TComponent : Component
        {
            if (type == TargetType.Target && targets != null)
            {
                bool succ = false;
                if (targets != null)
                    for (int i = 0; i < targets.Count; i++)
                        succ |= DoTarget(type, value, sender, targets[i], action);
                return succ;
            }
            else if (type == TargetType.TargetPlayer)
            {
                bool succ = false;
                if (targets != null)
                    for (int i = 0; i < targets.Count; i++)
                        succ |= DoTarget(type, value, sender, targets[i], action);
                return succ;
            }
            else
                return DoTarget(type, value, sender, -1, action);
        }

        public static bool AddTag(TargetType type, string tag, long self_id, long target_id)
            => DoTarget<string, ActorTagComp>(type, tag, self_id, target_id,
                static (_, c, e) => c.AddTag(e));
        public static bool RemoveTag(TargetType type, string tag, long self_id, long target_id)
            => DoTarget<string, ActorTagComp>(type, tag, self_id, target_id,
                static (_, c, e) => c.RemoveTag(e));
        public static bool AddTag(TargetType type, string tag, long self_id, List<long> targets)
            => DoTarget<string, ActorTagComp>(type, tag, self_id, targets,
                static (_, c, e) => c.AddTag(e));
        public static bool RemoveTag(TargetType type, string tag, long self_id, List<long> targets)
            => DoTarget<string, ActorTagComp>(type, tag, self_id, targets,
                static (_, c, e) => c.RemoveTag(e));


        public static bool AddAbility(TargetType type, int ability_id, long self_id, long target_id)
          => DoTarget<int, AbilityComp>(type, ability_id, self_id, target_id,
              static (_, c, e) => c.AddAbility(e));
        public static bool AddAbility(TargetType type, int ability_id, long self_id, List<long> targets)
            => DoTarget<int, AbilityComp>(type, ability_id, self_id, targets,
                static (_, c, e) => c.AddAbility(e));
        public static bool RemoveAbility(TargetType type, int ability_id, long self_id, long target_id)
            => DoTarget<int, AbilityComp>(type, ability_id, self_id, target_id,
                static (_, c, e) => c.RemoveAbility(e));
        public static bool RemoveAbility(TargetType type, int ability_id, long self_id, List<long> targets)
            => DoTarget<int, AbilityComp>(type, ability_id, self_id, targets,
                static (_, c, e) => c.RemoveAbility(e));
        static AbilityComp FindContext(long target)
        {
            var actor = Services.actor.Find(target);
            return actor.FindComponent<AbilityComp>();
        }
        private static void TriggerAbilitySelf(AbilityTriggerParam trigger)
        {
            var context = FindContext(trigger.sender);
            context?.Trigger(trigger);
        }
        public static void TriggerAbility(AbilityTriggerParam trigger)
        {
            switch (trigger.triggerType)
            {
                case TriggerType.Dead:
                case TriggerType.Born:
                case TriggerType.Add:
                case TriggerType.Remove:
                    TriggerAbilitySelf(trigger);
                    break;
                case TriggerType.Hit:
                    TriggerAbilitySelf(trigger);


                    {
                        if (trigger.hited != null)
                        {
                            trigger.triggerType = TriggerType.OnHit;
                            for (int i = 0; i < trigger.hited.Count; i++)
                            {
                                var context = FindContext(trigger.hited[i]);
                                context?.Trigger(trigger);
                            }
                        }
                    }
                    break;
                case TriggerType.OnHit:
                case TriggerType.Update:
                default:
                    break;
            }


        }



        public static bool AddBuff(TargetType type, int buff_id, long sender, long target_id) =>
            DoTarget<int, BuffComp>(type, buff_id, sender, target_id,
                static (sender, c, e) => c.AddBuff(e, sender) != -1);
        public static bool AddBuff(TargetType type, int buff_id, long sender, List<long> targets) =>
            DoTarget<int, BuffComp>(type, buff_id, sender, targets,
                static (sender, c, e) => c.AddBuff(e, sender) != -1);


        public static bool RemoveBuffByConfig(TargetType type, int buff_id, long sender, long target_id)
            => DoTarget<int, BuffComp>(type, buff_id, sender, target_id,
                static (_, c, e) => c.RemoveBuffByConfig(e));
        public static bool RemoveBuffByConfig(TargetType type, int buff_id, long sender, List<long> targets)
            => DoTarget<int, BuffComp>(type, buff_id, sender, targets,
                static (_, c, e) => c.RemoveBuffByConfig(e));




        public static void ModifySkill(string player, int skill, string rgName)
        {
            var context = Services.actor.FindPlayer(player);
            var _skill = context.skill;
            _skill.ModifyByName(skill, rgName);
        }
        public static void ModifySkill(string player, int skill, int rgIndex)
        {
            var context = Services.actor.FindPlayer(player);
            var _skill = context.skill;
            _skill.ModifyByIndex(skill, rgIndex);
        }
        public static SkillComp.Modify FindSkillModify(string player, int skill)
        {
            var context = Services.actor.FindPlayer(player);
            var _skill = context.skill;
            return _skill.FindModify(skill);
        }
    }
}




