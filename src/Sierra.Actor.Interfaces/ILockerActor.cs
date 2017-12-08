namespace Sierra.Actor.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// Defines a common contract for the LockerActor.
    /// </summary>
    public interface ILockerActor : IActor
    {
        Task QueueAdd(Type type, ActorId id);

        Task QueueRemove(Type type, ActorId id);
    }
}
