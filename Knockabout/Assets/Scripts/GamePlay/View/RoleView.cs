using GamePlay;
using IFramework;
using Luban;
using Lockstep;
namespace RGBC
{
    public class RoleView : ActorView<RoleActor>
    {

        class View
        {
            //FieldsStart
		public UnityEngine.Transform healthBar;

            //FieldsEnd

            public View(RoleView context)
            {
                //InitComponentsStart
			healthBar = context.GetTransform("healthBar@sm");

                //InitComponentsEnd
            }
        }

        private View view;
        protected override void InitComponents()
        {
            view = new View(this);
        }

        protected override void OnUpdate()
        {
        }

        public override void OnDead()
        {
        }

        protected override void OnInit()
        {
            var cfg = Configs.GetRole(this.actor.role_cfg_id);
        
            
        }

        protected override async AsyncTask OnDestroy(bool immediate)
        {
            await AsyncTask.CompletedTask;
        }
        public UnityEngine.Transform healthBarPos => view.healthBar;

        public override void SyncTransform()
        {
            transform.position = actor.transform.position.ToVector3();

        }
    }
}
