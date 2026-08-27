using System;

namespace GamePlay
{
    public struct SkillEnterCDEvent : IActorEvent_ForComp
    {
        public int skill_id;

        public SkillEnterCDEvent(int skill_id)
        {
            this.skill_id = skill_id;
        }

        public Type comp => typeof(SkillComp);
    }

}