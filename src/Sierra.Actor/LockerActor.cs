namespace Sierra.Actor
{
    using System;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public class LockerActor : Actor, ILockerActor
    {
        public LockerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public Task QueueAdd(Type type, ActorId id)
        {
            throw new NotImplementedException();
        }

        public Task QueueRemove(Type type, ActorId id)
        {
            throw new NotImplementedException();
        }
    }
}
