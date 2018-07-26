namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;

    /// <summary>
    /// The base actor class for Sierra, every actor should inherit from this directly or indirectly.
    /// </summary>
    /// <typeparam name="T">The root of the model that the actor deals with.</typeparam>
    public abstract class SierraActor<T> : Actor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SierraActor{T}"/>.
        /// </summary>
        /// <param name="actorService">The <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="ActorId"/> for this actor instance.</param>
        protected SierraActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// Adds <see cref="T"/> to the Sierra platform.
        /// </summary>
        /// <param name="model">The model of type <see cref="T"/> that we want to add.</param>
        /// <returns>The <see cref="Task"/> wrapper.</returns>
        public abstract Task Add(T model);

        /// <summary>
        /// Removes <see cref="T"/> to the Sierra platform.
        /// </summary>
        /// <param name="model">The model of type <see cref="T"/> that we want to remove.</param>
        /// <returns>The <see cref="Task"/> wrapper.</returns>
        public abstract Task Remove(T model);

        internal Dep GetDependentActor<Dep>() where Dep: IActor
        {
            return ActorProxy.Create<Dep>(ActorId.CreateRandom());
        }
    }
}
