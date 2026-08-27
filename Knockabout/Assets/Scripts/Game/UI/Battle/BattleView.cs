/*********************************************************************************
 *Author:         OnClick
 *Date:           2026-01-04
*********************************************************************************/
using IFramework;
using IFramework.UI;
using static EventDefine;
namespace RGBC
{
    public class BattleView : UIView, 
        IAsyncEventHandler<SyncHandCardFastArg>, 
        IEventHandler<AddCardArg>,
        IEventHandler<UseCardArg>
    {
        protected override void OnShow() { }
        protected override void OnHide() { }
        protected override void OnClose() { }
        class View
        {
            //FieldsStart
		public UnityEngine.UI.Button Set;
		public CardList cardList;

            //FieldsEnd
            public View(BattleView context)
            {
                //InitComponentsStart
			Set = context.GetComponent<UnityEngine.UI.Button>("Image/Set@sm");
			cardList = context.GetComponent<CardList>("Image/cardList@sm");

                //InitComponentsEnd
            }
        }
        private View view;
        [Inject] GameState_GamePlay state;
        protected override void InitComponents()
        {
            view = new View(this);
        }
        protected override void OnLoad() {
            this.RegisterEventHandlers();
            this.Bind(this.view.cardList.OnUseCard, (card) =>
            {
                state.Input_UseCard(card.index, card.card_id);
            });
        }





        async AsyncTask IAsyncEventHandler<SyncHandCardFastArg>.OnEvent(SyncHandCardFastArg message)
        {
            var cards = message.cards;
            view.cardList.Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                await AsyncTask.Delay(0.02f);
                view.cardList.AddCard(transform.position, cards[i]);
            }
        }

        void IEventHandler<AddCardArg>.OnEvent(AddCardArg message) => view.cardList.AddCard(message.pos, message.card);

        void IEventHandler<UseCardArg>.OnEvent(UseCardArg message)
        {
            this.view.cardList.RealUseCard(message.card_index);
        }
    }


}
