using Luban;

namespace GamePlay
{


    [Backup]
    public partial class PlayerActor : Actor
    {
        public override PropertyComp Property => property;
        [Backup] public PropertyComp_Player property;
        [Backup] public InputComp input;
        [Backup] public CardComp card;
        [Backup] public ActorModifyComp modify;
        [Backup] public BuffComp buff;
        [Backup] public SkillComp skill;
        [Backup] public AbilityComp ability;
        [Backup] public TransformComp transform;
        public PlayerData data { get; private set; }
        protected override void OnSetParam(CreateActorParam param)
        {
            
        }
        protected override void OnAwake()
        {
            modify = CreateComponent<ActorModifyComp>();
            ability = CreateComponent<AbilityComp>();
            property = CreateComponent<PropertyComp_Player>();
            buff = CreateComponent<BuffComp>();
            card = CreateComponent<CardComp>();
            input = CreateComponent<InputComp>();
            skill = CreateComponent<SkillComp>();
            transform = CreateComponent<TransformComp>();
            transform.radius = Configs.GetGlobal().HouseRadius;
            data = GameContext.gameData.FindPlayer(this.playerGUID);
            this.tags.AddTag(Tags.Player);
        }
        protected override void InitProperty()
        {
            property.ReadProperty(data.property);
        }
        protected override void OnEndReadBackUp()
        {
            data = GameContext.gameData.FindPlayer(playerGUID);
        }

        public void StartGame()
        {
            card.InitCards();
        }

    }
}


