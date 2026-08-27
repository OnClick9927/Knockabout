using System.Collections.Generic;

namespace GamePlay
{
    public interface IGameLogic : IService
    {
        void StartGame();
        void ExecuteInputs(List<PlayerInput> inputs);
    }
}


