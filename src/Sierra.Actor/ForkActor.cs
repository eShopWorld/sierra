namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using Common;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Common.Events;
    using Eshopworld.Core;
    using System.Linq;

    /// <summary>
    /// Manages Forks on behalf of tenant operations.
    /// </summary>
    [StatePersistence(StatePersistence.Persisted)]
    public class ForkActor : SierraActor<Fork>, IForkActor
    {
        private readonly GitHttpClient _gitClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly IBigBrother _bigBrother;
        private readonly SierraDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of <see cref="ForkActor"/>.
        /// </summary>
        /// <param name="actorService">The ActorService context.</param>
        /// <param name="actorId">The Actor ID.</param>
        /// <param name="gitClient">The <see cref="GitHttpClient"/> to use on repo operations.</param>
        /// <param name="vstsConfiguration">The VSTS configuration payload.</param>
        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration, IBigBrother bb, SierraDbContext sierraDbCtx)
            : base(actorService, actorId)
        {
            _gitClient = gitClient;
            _vstsConfiguration = vstsConfiguration;
            _bigBrother = bb;
            _dbContext = sierraDbCtx;
        }

        /// <inheridoc/>
        public override async Task Add(Fork fork)
        {
            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, fork);

            if (!repo.IsFork)
                _bigBrother.Publish(new ForkRequestFailed
                {
                    ForkName = repo.Name,
                    Message = $"Repository already exists but is not a fork"
                });
            else
            {
                if ((await _dbContext.Forks.FindAsync(repo.Id))==null)
                {
                    fork.ForkVstsId = repo.Id;
                    _dbContext.AttachSingular(fork);
                    await _dbContext.SaveChangesAsync();
                }
                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
            }
        }

        /// <inheridoc/>
        public override async Task Remove(Fork fork)
        {           
            var forkRemoved = await _gitClient.DeleteForkIfExists(fork.ToString());

            if (forkRemoved)
            {
                var dbFork = _dbContext.Forks.First(f => f.SourceRepositoryName == fork.SourceRepositoryName && f.TenantName == fork.TenantName);

                if (dbFork!=null)
                {
                    _dbContext.Forks.Remove(dbFork);
                    await _dbContext.SaveChangesAsync();

                }
                _bigBrother.Publish(new ForkDeleted { ForkName = fork.ToString() });
            }
            
        }       
    }
}
