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

    /// <summary>
    /// Manages Forks on behalf of tenant operations.
    /// </summary>
    [StatePersistence(StatePersistence.Persisted)]
    public class ForkActor : Actor, IForkActor
    {
        private readonly GitHttpClient _gitClient;
        private readonly VstsConfiguration _vstsConfiguration;

        /// <summary>
        /// Initializes a new instance of <see cref="ForkActor"/>.
        /// </summary>
        /// <param name="actorService">The ActorService context.</param>
        /// <param name="actorId">The Actor ID.</param>
        /// <param name="gitClient">The <see cref="GitHttpClient"/> to use on repo operations.</param>
        /// <param name="vstsConfiguration">The VSTS configuration payload.</param>
        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration)
            : base(actorService, actorId)
        {
            _gitClient = gitClient;
            _vstsConfiguration = vstsConfiguration;
        }

        /// <summary>
        /// Forks a source repository in VSTS.
        /// </summary>
        /// <param name="fork">The Fork payload containing all necessary information.</param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        public async Task ForkRepo(Fork fork)
        {
            var repo = (await _gitClient.GetRepositoriesAsync()).SingleOrDefault(r => r.Name == fork.SourceRepositoryName);
            if (repo == null) throw new ArgumentException($"Repository {fork.SourceRepositoryName} not found");

            await _gitClient.CreateFork(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, repo, fork.ForkSuffix);
        }     
    }
}
