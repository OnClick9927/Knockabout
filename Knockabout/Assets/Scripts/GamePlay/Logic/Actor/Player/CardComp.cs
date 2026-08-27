using ActionEditor.Nodes.BT;
using Lockstep;
using Luban;
using System.Collections.Generic;

namespace GamePlay
{
    [Backup]
    public partial class CardComp : Component<PlayerActor>, IUpdate
    {
        [Backup] public List<int> hand = new();
        [Backup] public LFloat NextGenCardTime;


        private void BuildNextGenCardTime()
        {
            NextGenCardTime = GameContext.state.time + Configs.GetGlobal().GenCardGap.ToLFloat();
        }
        protected override void OnAwake()
        {
            hand.Clear();
            BuildNextGenCardTime();
        }


        internal void InitCards()
        {

            var roles = actor.data.roles;
            for (int i = 0; i < roles.Count; i++)
            {
                var role_id = roles[i].id;
                var card = Configs.GetRole(role_id).GetCard();
                hand.Add(card.Id);
            }
            var g = Configs.GetGlobal();
            if (hand.Count < g.InitCardCount)
            {
                var count = g.InitCardCount - hand.Count;
                var id = g.InitCardID;
                for (int i = 0; i < count; i++)
                    hand.Add(id);
            }
            GameHelper.DoActorEvent(actor, new OnInitCardsEvent());

        }
        private void AddCard(int card_id, LVector3 pos)
        {
            if (hand.Count < Configs.GetGlobal().MaxCardCount)
            {
                hand.Add(card_id);
                GameHelper.DoActorEvent(actor, new OnAddCardEvent(card_id, pos, true));
            }
            else
            {
                GameHelper.DoActorEvent(actor, new OnAddCardEvent(card_id, pos, false));

            }
        }
        void IUpdate.Update()
        {

            if (GameContext.state.time >= NextGenCardTime)
            {
                var roles = this.actor.data.roles;
                if (roles.Count > 0)
                {
                    var index = GameContext.state.random.Range(0, roles.Count);
                    var card = Configs.GetRole(roles[index].id).GetCard();
                    AddCard(card.Id, this.actor.transform.position);
                }
                BuildNextGenCardTime();
            }
        }

        public void UseCard(int card_index, int card_id)
        {
            if (hand.Count <= card_index) return;
            if (card_index < 0) return;
            if (hand[card_index] != card_id) return;
            var cardData = Configs.GetCard(card_id);
            if (cardData.Cost > actor.property.coin) return;
            hand.RemoveAt(card_index);
            using (var scope = actor.property.BeginPropChange())
                scope.PushFixedProp(PropertyType.Coin, -cardData.Cost);
            GameHelper.DoActorEvent(actor, new OnUseCardEvent(card_id, card_index));
            if (cardData.Effect == CardType.Summon)
            {
                var playerData = GameContext.FindPlayer(this.player);
                var role = Services.actor.CreateActor<RoleActor>(CreateActorParam.Role(playerData,
                       cardData.Role));
                role.transform.SetPosition(actor.transform.position, true);
            }
        }
    }
}


