namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    //[StatePersistence(StatePersistence.Persisted)]
    //internal class LockedPrototypeActor : UniqueResourceActor<string>, ILockedPrototypeActor
    //{
    //    public LockedPrototypeActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
    //    {
    //    }

    //    internal override Task AddAction(string model)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    internal override Task RemoveAction(string model)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}
