using Lockstep;

namespace GamePlay
{
    [Backup]
    public partial class GameState
    {
        [Backup] public LFloat time { get; private set; }
        [Backup] public int speed { get; private set; }
        [Backup] public int uidIndex { get; private set; }
        [Backup] public long lastFrame { get; private set; }

        [Backup] public Lockstep.Random random { get; private set; }
        [Backup] public LFloat deltaTime { get; private set; }
        [Backup] public bool gameStart { get; private set; }

        [Backup] public bool paused { get; private set; }
        public void Pause() => paused = true;
        public void UnPause() => paused = false;



        public long currentFrame => lastFrame + 1;

        public GameState()
        {
            time = LFloat.zero;
            speed = LFloat.one;
            deltaTime = GameContext.logicDeltaTime.ToLFloat() * speed;
            uidIndex = 0;
            lastFrame = -1;
            gameStart = false;
            paused = true;
        }

        public int GenUid() => ++uidIndex;
        public void SetSpeed(int value) => speed = value;
        public void ResetSpeed() => SetSpeed(1);

        public void AddDeltaTime() {
            deltaTime = GameContext.logicDeltaTime.ToLFloat() * speed;
            time += deltaTime;
        }

        internal void SetLastFrame(long frame) => lastFrame = frame;

        internal void StartGame()
        {
            gameStart = true;
            random = new Lockstep.Random((uint)GameContext.gameData.randomSeed);
        }
    }
}


