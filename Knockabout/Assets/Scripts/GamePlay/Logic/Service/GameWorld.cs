using ActionBuffer;
using Lockstep;
using System.Collections.Generic;
using System.Text;
using static GamePlay.Services;
namespace GamePlay
{
    public class GameWorld : Service, IGameWorld, IUpdate
    {


        private List<Service> services;

        protected override void OnInit()
        {
            services = new();
            CreateService<ActorService>();
            CreateService<GameLogicService>();
            CreateService<CollisionService>();
            CreateService<RvoService>();
            GameContext.InitGame();
        }

        void IUpdate.Update()
        {
            if (!GameContext.state.gameStart) return;
            if (GameContext.state.paused) return;
            LoopAgain:
            var count = services.Count;
            for (int i = 0; i < count; i++)
            {
                if (services[i] is IUpdate update)
                    update.Update();
            }
            if (FrameLoop())
                goto LoopAgain;
        }
        protected override void OnDispose()
        {
            if (services == null || services.Count == 0) return;
            using (var pool = StaticPool.CreateDisposable<Queue<IService>>())
            {
                pool.value.Clear();
                var count = services.Count;
                for (int i = 0; i < count; i++)
                    pool.value.Enqueue(services[i]);
                for (int i = 0; i < count; i++)
                {
                    var sys = pool.value.Dequeue();
                    Services.Remove(sys);
                }
            }
        }

        T CreateService<T>() where T : Service, new()
        {
            T sys = new T();
            sys.Init();
            services.Add(sys);

            return sys;
        }






        public bool chasingFrame { get; private set; }

        void IGameWorld.StartGame()
        {

            GameContext.StartGame();
            game_logic.StartGame();

            helper.SendPlayerInputToServer();

        }





        bool FrameLoop()
        {
            if (!GameContext.state.gameStart) return false;
            GameContext.state.AddDeltaTime();

            var inputs = GetInputs();
            var currentFrame = GameContext.state.currentFrame;
            if (inputs != null)
            {
                game_logic.ExecuteInputs(inputs);
                WriteBackup();
                GameContext.state.SetLastFrame(currentFrame);
                if (!chasingFrame)
                    helper.SendPlayerInputToServer();
                var nextInputs = GetInputs();
                chasingFrame = nextInputs != null;
                return chasingFrame;
            }
            else
            {
                helper.Log($"Prediction Inputs_{currentFrame}  {inputs_Prediction.Count}/{GameContext.MaxPredictionFrameCount}");
                if (inputs_Prediction.Count >= GameContext.MaxPredictionFrameCount)
                {
                    helper.Error("Prediction Inputs So Long");
                    GameContext.state.Pause();
                    return false;
                }



                chasingFrame = false;
                var pre = PredictionInputs(currentFrame);
                inputs_Prediction[currentFrame] = pre;
                game_logic.ExecuteInputs(pre);
                WriteBackup();
                GameContext.state.SetLastFrame(currentFrame);

                helper.SendPlayerInputToServer();
                return false;
            }


        }
        private List<PlayerInput> PredictionInputs(long frame)
        {
            return new List<PlayerInput>() { };
        }
        private void CheckRollBack(long frame, List<PlayerInput> inputs)
        {
            if (!inputs_Prediction.TryGetValue(frame, out var pre)) return;
            bool success = inputs.Count == pre.Count;
            if (success)
            {
                for (int i = 0; i < pre.Count; i++)
                {
                    if (pre[i].Equals(inputs[i])) continue;
                    success = false;
                    break;
                }
            }




            if (success)
            {
                inputs_Prediction.Remove(frame);
            }
            else
            {
                inputs_Prediction.Clear();
                var times = (frame - 1) % GameContext.backUpFrameCount;
                RollBackTo(frame - 1 - times);
            }
        }


        void IGameWorld.OnRecPlayerInput(List<PlayerInput> inputs)
        {
            var frmae = inputs[0].frame;
            if (frmae <= GameContext.state.lastFrame - GameContext.MaxPredictionFrameCount)
                return;
            this.inputs[frmae] = inputs;
            CheckRollBack(frmae, inputs);
        }

        private Dictionary<long, List<PlayerInput>> inputs = new();
        private Dictionary<long, List<PlayerInput>> inputs_Prediction = new();


        private List<PlayerInput> GetInputs() => inputs.TryGetValue(GameContext.state.currentFrame, out var result) ? result : null;







        private Dictionary<long, BufferWriter> backs = new Dictionary<long, BufferWriter>();
        void WriteBackup()
        {
            if (GameContext.GameType == GameType.Local) return;
            var currentFrame = GameContext.state.currentFrame;

            if (currentFrame % GameContext.backUpFrameCount != 0) return;
            BufferWriter writer = new BufferWriter();

            backs[currentFrame] = writer;
            GameContext.WriteBackup(writer);

        }
        void RollBackTo(long frame)
        {
            if (!backs.TryGetValue(frame, out var writer)) return;

            collision.ClearAgents();
            rvo.ClearAgents();
            helper.Log($"RollBackTo Frame_{frame}");
            BufferReader reader = new BufferReader();
            reader.Init(writer.buffer);
            GameContext.ReadBackup(reader);
            actor.EndReadBackUp();
            view.DestroyUseLessActorView();

        }

        string IGameWorld.Dump() => GameContext.DumpString(new StringBuilder(), string.Empty).ToString();
        int IGameWorld.GetHash()
        {
            var idx = 0;
            return GameContext.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);
        }

    }
}


