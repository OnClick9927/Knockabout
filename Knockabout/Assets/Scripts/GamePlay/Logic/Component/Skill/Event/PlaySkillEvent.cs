using System;
using System.Collections.Generic;

namespace GamePlay
{
    public struct PlaySkillEvent : IActorEvent_ForComp
    {
        public Type comp => typeof(SkillComp);

        public int skill_id;
        public List<long> hit;

        public PlaySkillEvent(int skill_id, List<long> hit)
        {
            this.skill_id = skill_id;
            this.hit = hit;
        }
    }

}