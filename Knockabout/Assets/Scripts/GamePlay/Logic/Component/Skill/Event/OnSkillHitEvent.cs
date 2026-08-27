using System.Collections.Generic;

namespace GamePlay
{
    public struct OnSkillHitEvent : IActorEvent
    {
        public int skill_id;
        public List<long> hit;

        public OnSkillHitEvent(int skill_id, List<long> hit)
        {
            this.skill_id = skill_id;
            this.hit = hit;
        }
    }

}