namespace GamePlay
{
    [Backup]
    public partial class InputComp : Component<PlayerActor>
    {

        protected override void OnAwake()
        {

        }

        public void ExecuteInput(PlayerInput input)
        {
            switch (input.type)
            {
                case PlayerInput.InputType.None:
                    return;
                case PlayerInput.InputType.UseCard:
                    UseCard(input);
                    break;
                default:
                    break;
            }
        }
        private void UseCard(PlayerInput input)
        {
            actor.card.UseCard(input.Card_index, input.Card_id);
        }

    }
}


