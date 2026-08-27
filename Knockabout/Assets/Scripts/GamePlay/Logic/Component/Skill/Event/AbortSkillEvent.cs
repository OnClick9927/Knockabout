using System;

namespace GamePlay
{
    public struct AbortSkillEvent : IActorEvent_ForComp
    {
        public Type comp => typeof(SkillComp);

        public int skill_id;

        public AbortSkillEvent(int skill_id)
        {
            this.skill_id = skill_id;
        }
    }

}