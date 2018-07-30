namespace Sierra.Actor
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using Microsoft.ServiceFabric.Actors.Client;

    /// <summary>
    /// The main tenant orchestration actor.
    /// This guy handles the full tenant change workflow, maps to the API verb usage and keeps track of all internal state.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    internal class TenantActor : SierraActor<Tenant>, ITenantActor
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
            // Flow
            if (tenant == null)
                return;

            // #1 Fork anything that needs to be forked
            await ProcessForks(tenant.Name, tenant.CustomSourceRepos);
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

        private async Task ProcessForks(string tenantName, IEnumerable<string> customSourceRepos)
        {
            var forkActor = ActorProxy.Create<IForkActor>(ActorId.CreateRandom());

            var existingTenantRepos = new List<Fork>(); //TODO: plug into the state when available
            
            //create repos with tenant name as suffix
            var customForkList = customSourceRepos.Select(r => new Fork { SourceRepositoryName = r, TenantName = tenantName });
            await Task.WhenAll(customForkList.Select(r => forkActor.Add(r)));
            //delete orphaned forks
            var orphanedList = existingTenantRepos.Except(customForkList);
            await Task.WhenAll(orphanedList.Select(r => forkActor.Remove(r)));
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
    }
}
