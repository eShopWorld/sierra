namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;

    /// <summary>
    /// The main tenant orchestration actor.
    /// This guy handles the full tenant change workflow, maps to the API verb usage and keeps track of all internal state.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    [ActorService(Name = nameof(TenantActor))]
    internal class TenantActor : SierraActor, ITenantActor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TenantActor"/>.
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public TenantActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public override async Task Add(Tenant tenant)
        {
            await Task.Yield(); // todo: temporary
        }

        public override async Task Remove(Tenant tenant)
        {
            await Task.Yield(); // todo: temporary
        }
    }
}
