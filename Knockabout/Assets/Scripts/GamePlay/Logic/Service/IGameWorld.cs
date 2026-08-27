using System.Collections.Generic;

namespace GamePlay
{


    public interface IGameWorld : IService
    {
        bool chasingFrame { get; }
        void OnRecPlayerInput(List<PlayerInput> inputs);
        void StartGame();
        string Dump();
        int GetHash();
    }
}


