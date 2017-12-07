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

        /// <summary>
        /// Adds a tenant to the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="T:System.Threading.Tasks.Task" /> wrapper.</returns>
        public override async Task Add(Tenant tenant)
        {
            await Task.Yield(); // todo: temporary

            // Flow

            // #1 Fork anything that needs to be forked
            // #2 Create CI builds for each new fork created for the tenant
            // #3 Build the tenant test resources
            // #4 Build the tenant production resources
            // #5 Release definition
                // #5a Create a release definition from dev
                // #5a If there are forks, create a release definition from master
                // #5b If there are no forks, put the tenant into a ring on the global master release definition
            // #6 Create the tenant Azure AD application for test and prod
            // #7 Map the tenant KeyVault for all test environments and prod
        }

        /// <summary>
        /// Removes a tenant from the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public override async Task Remove(Tenant tenant)
        {
            await Task.Yield(); // todo: temporary
        }

        /// <summary>
        /// Changes a tenant in the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public async Task Edit(Tenant tenant)
        {
            await Task.Yield(); // todo: temporary
        }
    }
}
