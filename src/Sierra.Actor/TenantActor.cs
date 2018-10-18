namespace Sierra.Actor
{
    using System.Linq;
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
        public override async Task<Tenant> Add(Tenant tenant)
        {
            // Flow
            if (tenant == null)
                return null;

            var dbTenant = await _dbContext.LoadCompleteTenantAsync(tenant.Code);
            if (dbTenant == null) //new tenant
            {
                dbTenant = new Tenant(tenant.Code);
                _dbContext.Tenants.Add(dbTenant);
            }
            
            dbTenant.Update(tenant);
            //persist "ToBeDeleted"+"ToBeCreated" records
            await _dbContext.SaveChangesAsync();

            // #1 sync forks (add + remove)
            await Task.WhenAll(dbTenant.CustomSourceRepos
                .Where(f => f.State== EntityStateEnum.NotCreated)
                .Select(f =>
                    GetActor<IForkActor>(f.ToString()).Add(f)
                        .ContinueWith((t) => f.Update(t.Result), TaskContinuationOptions.NotOnFaulted)));

            await Task.WhenAll(dbTenant.CustomSourceRepos
                .Where(f => f.State == EntityStateEnum.ToBeDeleted)
                .Select(f =>
                    GetActor<IForkActor>(f.ToString()).Remove(f)
                        .ContinueWith((t) => _dbContext.Entry(f).State = Microsoft.EntityFrameworkCore.EntityState.Deleted, TaskContinuationOptions.NotOnFaulted)));

            // #2 Sync CI builds for forks created for the tenant (add + remove)
            await Task.WhenAll(dbTenant.BuildDefinitions
                .Where(d => d.State == EntityStateEnum.NotCreated)
                .Select(d =>
                    GetActor<IBuildDefinitionActor>(d.ToString()).Add(d)
                        .ContinueWith((t) => d.Update(t.Result), TaskContinuationOptions.NotOnFaulted)));

            await Task.WhenAll(dbTenant.BuildDefinitions
                .Where(d => d.State == EntityStateEnum.ToBeDeleted)
                .Select(d =>
                    GetActor<IBuildDefinitionActor>(d.ToString()).Remove(d)
                        .ContinueWith((t) => _dbContext.Entry(d).State = Microsoft.EntityFrameworkCore.EntityState.Deleted, TaskContinuationOptions.NotOnFaulted)));

            // #3 Build the tenant test resources
            // #4 Build the tenant production resources
            // #5 Release definition
            // #5a Create a release definition from dev
            // #5a If there are forks, create a release definition from master

            await Task.WhenAll(dbTenant.ReleaseDefinitions
                .Where(d => d.State == EntityStateEnum.NotCreated)
                .Select(d =>
                    GetActor<IReleaseDefinitionActor>(d.ToString()).Add(d)
                        .ContinueWith((t) => d.Update(t.Result), TaskContinuationOptions.NotOnFaulted)));               
            
            await Task.WhenAll(dbTenant.ReleaseDefinitions
                .Where(d => d.State == EntityStateEnum.ToBeDeleted)
                .Select(d =>
                    GetActor<IReleaseDefinitionActor>(d.ToString()).Remove(d)
                        .ContinueWith((t) => _dbContext.Entry(d).State = Microsoft.EntityFrameworkCore.EntityState.Deleted, TaskContinuationOptions.NotOnFaulted)));

            // #5b If there are no forks, put the tenant into a ring on the global master release definition
            // #6 Create the tenant Azure AD application for test and prod
            // #7 Map the tenant KeyVault for all test environments and prod

            //final state persistence
            await _dbContext.SaveChangesAsync();
            return dbTenant;
        }

        /// <summary>
        /// Removes a tenant from the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public override async Task Remove(Tenant tenant)
        {
            tenant = await _dbContext.LoadCompleteTenantAsync(tenant.Code);
            if (tenant == null)
                return;

            await Task.WhenAll(
                tenant.CustomSourceRepos.Select(f => GetActor<IForkActor>(f.ToString()).Remove(f)
                    .ContinueWith(t => _dbContext.Entry(f).State = Microsoft.EntityFrameworkCore.EntityState.Deleted, TaskContinuationOptions.NotOnFaulted)));

            await Task.WhenAll(
                tenant.BuildDefinitions.Select(bd => GetActor<IBuildDefinitionActor>(bd.ToString()).Remove(bd)
                    .ContinueWith(t => _dbContext.Entry(bd).State = Microsoft.EntityFrameworkCore.EntityState.Deleted, TaskContinuationOptions.NotOnFaulted)));

            _dbContext.Remove(tenant);
            await _dbContext.SaveChangesAsync();
        }
    }
}
