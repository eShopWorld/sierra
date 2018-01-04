namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    /// <summary>
    /// The base class for any actor that is locked (unique) in the scope of any resource.
    /// </summary>
    /// <typeparam name="T">The root of the model that the actor deals with.</typeparam>
    public abstract class UniqueResourceActor<T> : SierraActor<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UniqueResourceActor{T}"/>.
        /// </summary>
        /// <param name="actorService">The <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="ActorId"/> for this actor instance.</param>
        protected UniqueResourceActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <inheritdoc />
        public override Task Add(T model)
        {
            // Queue work in the locker
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override Task Remove(T model)
        {
            // Queue work in the locker
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Internal call back used to run the Add action, assuming the Add can be queued based on a given resource.
        /// </summary>
        /// <param name="model">The model of type <see cref="T"/> that we want to add.</param>
        /// <returns>The <see cref="Task"/> wrapper.</returns>
        internal abstract Task AddAction(T model);

        /// <summary>
        /// Internal call back used to run the Remove action, assuming the Remove can be queued based on a given resource.
        /// </summary>
        /// <param name="model">The model of type <see cref="T"/> that we want to remove.</param>
        /// <returns>The <see cref="Task"/> wrapper.</returns>
        internal abstract Task RemoveAction(T model);
    }
}
