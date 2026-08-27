using System;

namespace GamePlay
{
    public interface IActorEvent_ForComp:IActorEvent
    {
        Type comp { get; }
    }
}