using System;

namespace GamePlay
{


    public abstract class Service : IService, IDisposable
    {
        protected Service()
        {
            Services.Add(this);
        }
        public void Init() => OnInit();
        protected abstract void OnInit();
        protected abstract void OnDispose();
        void IDisposable.Dispose()
        {
            //Units.Remove(this);
            OnDispose();
        }


    }
}
