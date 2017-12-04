namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;

    abstract class SierraActor : Actor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SierraActor"/>.
        /// </summary>
        /// <param name="actorService">The <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="ActorId"/> for this actor instance.</param>
        protected SierraActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public abstract Task Add(Tenant tenant);

        public abstract Task Remove(Tenant tenant);
    }
}
