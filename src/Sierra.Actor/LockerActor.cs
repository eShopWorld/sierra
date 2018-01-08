namespace Sierra.Actor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    [StatePersistence(StatePersistence.Persisted)]
    public class LockerActor : Actor, ILockerActor
    {
        //internal IDictionary<Type, Queue<string>> Work = new ConcurrentDictionary<Type, Queue<string>>();

        public LockerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        //protected override Task OnActivateAsync()
        //{
        //    // Deserialize from StateManager
        //    return base.OnActivateAsync();
        //}

        //protected override Task OnDeactivateAsync()
        //{
        //    // Serialize to StateManager
        //    return base.OnDeactivateAsync();
        //}

        public async Task QueueAdd(string type, ActorId id)
        {
            await Task.Yield();
        }

        public async Task QueueRemove(string type, ActorId id)
        {
            await Task.Yield();
        }
    }
}
