/*********************************************************************************
*Author:         OnClick
*Date:           2026-05-22
*********************************************************************************/
using GamePlay;
using IFramework;
using Lockstep;

namespace RGBC
{
    public class HouseView : ActorView<PlayerActor>
    {

        class View
        {
            //FieldsStart
            public UnityEngine.Transform healthBar;

            //FieldsEnd

            public View(HouseView context)
            {
                //InitComponentsStart
                healthBar = context.GetTransform("healthBar@sm");

                //InitComponentsEnd
            }
        }

        private View view;
        public UnityEngine.Transform healthBarPos => view.healthBar;
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

        }

        protected override AsyncTask OnDestroy(bool immediate)
        {
            return AsyncTask.CompletedTask;
        }

        public override void SyncTransform()
        {
            transform.position = actor.transform.position.ToVector3();

        }
        public void OnAddCardByPlayer(OnAddCardEvent add)
        {
            if (!actor.IsLocalPlayer) return;
            Events.Publish(new EventDefine.AddCardArg(add.card_id, add.pos.ToVector3()));
        }

        public void OnUseCard(OnUseCardEvent use)
        {
            if (!actor.IsLocalPlayer) return;
            Events.Publish(new EventDefine.UseCardArg(use.card_index, use.card_id));

        }

        public void SyncHandCardFast()
        {
            if (!actor.IsLocalPlayer) return;
            var cards = actor.card.hand;
            Events.Publish(new EventDefine.SyncHandCardFastArg(cards));

        }
    }
}
