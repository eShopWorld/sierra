namespace Sierra.Actor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using Common;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Eshopworld.Telemetry;
    using Common.Events;

    /// <summary>
    /// Manages Forks on behalf of tenant operations.
    /// </summary>
    [StatePersistence(StatePersistence.Persisted)]
    public class ForkActor : Actor, IForkActor
    {
        private readonly GitHttpClient _gitClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly BigBrother _bigBrother;

        /// <summary>
        /// Initializes a new instance of <see cref="ForkActor"/>.
        /// </summary>
        /// <param name="actorService">The ActorService context.</param>
        /// <param name="actorId">The Actor ID.</param>
        /// <param name="gitClient">The <see cref="GitHttpClient"/> to use on repo operations.</param>
        /// <param name="vstsConfiguration">The VSTS configuration payload.</param>
        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration, BigBrother bb)
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
        public async Task ForkRepo(Fork fork)
        {           
            var sourceRepo = (await _gitClient.GetRepositoriesAsync()).SingleOrDefault(r => r.Name == fork.SourceRepositoryName);
            if (sourceRepo == null) throw new ArgumentException($"Repository {fork.SourceRepositoryName} not found");               

            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, sourceRepo, fork.ForkSuffix);

            if (!repo.IsFork || !repo.ProjectReference.Id.Equals(sourceRepo.ProjectReference.Id))
                _bigBrother.Publish(new ForkRequestFailed
                {
                    ForkName = repo.Name,
                    Message = $"Repository already exists but is not a fork or is a fork of another project"
                });
            else         
                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
        }     
    }
}
