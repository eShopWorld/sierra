namespace Sierra.Actor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public class LockerActor : Actor, ILockerActor
    {
        internal IDictionary<Type, Queue<string>> Work = new ConcurrentDictionary<Type, Queue<string>>();

        public LockerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override Task OnActivateAsync()
        {
            // Deserialize from StateManager
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            // Serialize to StateManager
            return base.OnDeactivateAsync();
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
