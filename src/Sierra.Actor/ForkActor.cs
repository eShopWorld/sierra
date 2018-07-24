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

    /// <summary>
    /// Manages Forks on behalf of tenant operations.
    /// </summary>
    [StatePersistence(StatePersistence.Persisted)]
    public class ForkActor : Actor, IForkActor
    {
        private readonly GitHttpClient _gitClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly IBigBrother _bigBrother;

        /// <summary>
        /// Initializes a new instance of <see cref="ForkActor"/>.
        /// </summary>
        /// <param name="actorService">The ActorService context.</param>
        /// <param name="actorId">The Actor ID.</param>
        /// <param name="gitClient">The <see cref="GitHttpClient"/> to use on repo operations.</param>
        /// <param name="vstsConfiguration">The VSTS configuration payload.</param>
        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration, IBigBrother bb)
            : base(actorService, actorId)
        {
            _gitClient = gitClient;
            _vstsConfiguration = vstsConfiguration;
            _bigBrother = bb;
        }

        /// <summary>
        /// Forks a source repository in VSTS.
        /// </summary>
        /// <param name="fork">The Fork payload containing all necessary information.</param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public async Task AddFork(Fork fork)
        {
            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, fork.SourceRepositoryName, fork.ForkSuffix);

            if (!repo.IsFork)
                _bigBrother.Publish(new ForkRequestFailed
                {
                    ForkName = repo.Name,
                    Message = $"Repository already exists but is not a fork"
                });
            else         
                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
        }

        /// <summary>
        /// remove an existing repo (if exists)
        /// </summary>
        /// <param name="forkName">name of the repo to remove</param>
        /// <returns>task instance</returns>
        public async Task RemoveFork(string forkName)
        {           
            var forkRemoved = await _gitClient.DeleteForkIfExists(forkName);

            if (forkRemoved)
                _bigBrother.Publish(new ForkDeleted { ForkName = forkName });
            
        }
    }
}
