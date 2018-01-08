namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// Defines a common contract for the LockerActor.
    /// </summary>
    public interface ILockerActor : IActor
    {
        Task QueueAdd(string type, ActorId id);

        Task QueueRemove(string type, ActorId id);
    }
}
