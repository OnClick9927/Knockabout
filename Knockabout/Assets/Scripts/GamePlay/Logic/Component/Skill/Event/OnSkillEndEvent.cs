namespace GamePlay
{

    public struct OnSkillEndEvent : IActorEvent
    {
        public int skill_id;

        public OnSkillEndEvent(int skill_id)
        {
            this.skill_id = skill_id;
        }
    }

}