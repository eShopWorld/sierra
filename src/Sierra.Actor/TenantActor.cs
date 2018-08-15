namespace Sierra.Actor
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// The main tenant orchestration actor.
    /// This guy handles the full tenant change workflow, maps to the API verb usage and keeps track of all internal state.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    internal class TenantActor : SierraActor<Tenant>, ITenantActor
    {
        private readonly SierraDbContext _dbContext;
        
        /// <summary>
        /// Initializes a new instance of <see cref="TenantActor"/>.
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="sierraDbCtx">sierra db context</param>
        public TenantActor(ActorService actorService, ActorId actorId, SierraDbContext sierraDbCtx)
            : base(actorService, actorId)
        {
            _dbContext = sierraDbCtx;
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

            Tenant dbTenant = null;
            if ((dbTenant = await _dbContext.LoadCompleteTenant(tenant.Code)) == null) //new tenant
            {
                dbTenant = new Tenant(tenant.Code);
                _dbContext.Tenants.Add(dbTenant);
            }
            
            dbTenant.Update(tenant);          

            await _dbContext.SaveChangesAsync();
            
            // #1 Fork anything that needs to be forked
            await HandleForks(dbTenant);
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

        private async Task HandleForks(Tenant tenant)
        {
            await Task.WhenAll(tenant.ForksToAdd.Select(r => GetActor<IForkActor>(r.ToString()).Add(r)));
            await Task.WhenAll(tenant.ForksToRemove.Select(r => GetActor<IForkActor>(r.ToString()).Remove(r)));
        }


        /// <summary>
        /// Removes a tenant from the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public override async Task Remove(Tenant tenant)
        {
            tenant = await _dbContext.LoadCompleteTenant(tenant.Code);
            if (tenant == null)
                return;

            await RemoveForks(tenant.CustomSourceRepos);
            _dbContext.Remove(tenant);
            await _dbContext.SaveChangesAsync();
        }

        private async Task RemoveForks(IEnumerable<Fork> forks)
        {
            await Task.WhenAll(forks.Select(f => 
                GetActor<IForkActor>(f.ToString()).Remove(f)
                .ContinueWith((t) => _dbContext.Entry(f).State = EntityState.Detached, TaskContinuationOptions.NotOnFaulted)//TODO: review this
            ));
        }
    }
}
