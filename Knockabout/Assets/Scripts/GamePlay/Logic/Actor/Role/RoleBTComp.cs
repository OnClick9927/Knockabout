using Luban;

namespace GamePlay
{
    [Backup]
    public partial class RoleBTComp : Component<RoleActor>, IUpdate
    {
        [Backup] public ActorBTBlackBoard blackboard;
        private ActorBTAsset bt;
        protected override void OnReset()
        {
            base.OnReset();
            blackboard = null;
            bt = null;

        }
        protected override void OnEndReadBackUp()
        {
            base.OnEndReadBackUp();
            var roleCfg = Luban.Configs.GetRole(actor.role_cfg_id);
            var roleLevCfg = roleCfg.LevConfig(actor.role_lv);
            bt = Services.helper.LoadRoleBTAsset(roleCfg, roleLevCfg);
            blackboard.Initialize(bt, blackboard._RuntimeValues);
            blackboard.actor = actor;
        }
        public void LoadBT(Luban.Role roleCfg, Luban.RoleLev roleLevCfg)
        {
            this.bt = Services.helper.LoadRoleBTAsset(roleCfg, roleLevCfg);
            this.blackboard = new ActorBTBlackBoard();
            this.blackboard.CopyFieldsFrom(this.bt.blackboard);
            this.blackboard.actor = this.actor;
            this.blackboard.Initialize(this.bt);
        }

        protected override void OnBeginWriteBackUp()
        {
            base.OnBeginWriteBackUp();
            SaveRuntimeValues();
        }

        private void SaveRuntimeValues()
        {
            var runtimeValues = blackboard.RuntimeValues;
            blackboard._RuntimeValues.Clear();
            for (int i = 0; i < runtimeValues.Count; i++)
                blackboard._RuntimeValues.Add(runtimeValues[i]);
        }


        protected override void OnAwake()
        {
            if (actor.IsBackup)
                blackboard = new ActorBTBlackBoard();
        }

        void IUpdate.Update()
        {
            if (this.bt == null) return;
            this.bt.Update(blackboard);
        }
    }
}
