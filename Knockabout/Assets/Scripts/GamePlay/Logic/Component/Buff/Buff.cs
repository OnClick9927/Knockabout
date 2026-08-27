using Lockstep;
using static GamePlay.BuffAsset.Buff;
namespace GamePlay
{
    [Backup]
    public partial class Buff
    {
        [Backup] public long uid;

        [Backup] public int cfg_id;

        [Backup] public long target { get; set; }
        [Backup] public long sender { get; set; }

        [Backup] public int layer { get; set; }

        [Backup] public LFloat EndTime { get; private set; }
        [Backup] public LFloat TriggerTime { get; private set; }

        internal Actor actor;
        internal Actor actor_sender;


        public BuffAsset.Buff data { get; private set; }

        public void ReadCfg()
        {
            data = Services.helper.LoadBuff(cfg_id);
        }
        private void CalcJumpTime()
        {
            if (data.trigger == TriggerType.Jump)
                TriggerTime = data.TriggerGap.ToLFloat() + GameContext.state.time;
        }
        private void CalcEndTime()
        {
            if (data.removeType == RemoveType.Time)
            {
                EndTime = GameContext.state.time + data.Life.ToLFloat();
                GameHelper.DoActorEvent(actor,
                    new OnBuffChangeEvent(OnBuffChangeEvent.Type.EndTime, this));
            }

        }

        public void OnRemove()
        {
            for (int i = 0; i < data.Effects.Count; i++)
            {
                var effect = data.Effects[i];
                if (effect.trigger == BuffEffect.TriggerType.Remove)
                    effect.DoEffect(this);
            }
            GameHelper.DoActorEvent(actor,
                new OnBuffChangeEvent(OnBuffChangeEvent.Type.Remove, this));


        }

        public void OnAdd()
        {
            for (int i = 0; i < data.Effects.Count; i++)
            {
                var effect = data.Effects[i];
                if (effect.trigger == BuffEffect.TriggerType.Add)
                    effect.DoEffect(this);
            }
            GameHelper.DoActorEvent(actor,
               new OnBuffChangeEvent(OnBuffChangeEvent.Type.Add, this));
            CalcEndTime();
            CalcJumpTime();
        }

        public void OnAddLayer()
        {
            var add = layer < data.MaxLayers;
            if (add)
            {
                this.layer++;
                GameHelper.DoActorEvent(actor,
                   new OnBuffChangeEvent(OnBuffChangeEvent.Type.AddLayer, this));
            }
            CalcEndTime();

        }
        private void OnMinusLayer()
        {
            var minus = layer > 1 && data.addType == AddType.Layers;
            if (minus)
            {
                this.layer--;
                GameHelper.DoActorEvent(actor,
                   new OnBuffChangeEvent(OnBuffChangeEvent.Type.MinusLayer, this));

            }
            CalcEndTime();
        }

        public void Update()
        {
            if (data.trigger != TriggerType.Jump) return;
            if (TriggerTime > GameContext.state.time) return;
            CalcJumpTime();
            for (int i = 0; i < data.Effects.Count; i++)
            {
                var effect = data.Effects[i];
                if (effect.trigger == BuffEffect.TriggerType.Jump)
                    effect.DoEffect(this);
            }
        }

        public bool NeedRemoveByTimeNow()
        {
            if (data.removeType == RemoveType.None) return false;
            if (data.addType == AddType.Layers)
            {
                if (this.layer > 1)
                {
                    if (GameContext.state.time >= this.EndTime)
                        OnMinusLayer();
                    return false;
                }
                return true;
            }
            return GameContext.state.time >= this.EndTime;
        }


    }
}


