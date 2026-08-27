using Luban;

namespace GamePlay
{
    [Backup]
    public partial class RoleActor : Actor
    {
        [Backup] public BuffComp buff { get; private set; }
        [Backup] public SkillComp skill { get; private set; }
        [Backup] public AbilityComp ability { get; private set; }

        [Backup] public TransformComp transform { get; private set; }
        [Backup] public MoveComp move { get; private set; }

        [Backup] public RoleBTComp bt;
        [Backup] public int role_cfg_id { get; private set; }
        [Backup] public int role_lv { get; private set; }
        [Backup] public PropertyComp_Role property;
        public override PropertyComp Property => property;

        public Role roleCfg {  get; private set; }
        public RoleLev roleLevCfg {  get; private set; }
        protected override void OnSetParam(CreateActorParam param)
        {
            role_cfg_id = param.roleInfo.id;
            role_lv = param.roleInfo.level;
        }
        protected override void OnAwake()
        {
            this.buff = CreateComponent<BuffComp>();
            this.ability = CreateComponent<AbilityComp>();
            this.skill = CreateComponent<SkillComp>();
            this.transform = CreateComponent<TransformComp>();
            this.bt = CreateComponent<RoleBTComp>();
            this.move = CreateComponent<MoveComp>();
            this.tags.AddTag(Tags.Role);
            if (IsBackup) return;
            LoadConfig();
            this.bt.LoadBT(roleCfg, roleLevCfg);
        }
        private void LoadConfig()
        {
            roleCfg = Luban.Configs.GetRole(role_cfg_id);
            roleLevCfg = roleCfg.LevConfig(role_lv);
            this.transform.radius = roleCfg.BoxRadius;
        }
        protected override void OnEndReadBackUp() => LoadConfig();
        protected override void InitProperty()
        {
            var prop = roleLevCfg.Property;
            this.property.ReadProperty(prop);
        }

    
    }
}
