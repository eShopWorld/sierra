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

    [StatePersistence(StatePersistence.Volatile)]
    internal class ForkActor : Actor, IForkActor
    {
        private readonly GitHttpClient _gitClient;
        private readonly VstsConfiguration _vstsConfiguration;

        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration) : base(actorService, actorId)
        {
            _gitClient = gitClient;
            _vstsConfiguration = vstsConfiguration;
        }

        /// <summary>
        /// for the source repository
        /// </summary>
        /// <param name="fork">request payload</param>
        /// <returns>Task instance</returns>
        public async Task ForkRepo(Fork fork)
        {
            var repos = await _gitClient.GetRepositoriesAsync();

            var sourceRepo = repos.SingleOrDefault(r => r.Name == fork.SourceRepositoryName);
            if (sourceRepo == null)
                throw new Exception($"Repository {fork.SourceRepositoryName} not found");

            await _gitClient.CreateFork(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, sourceRepo, fork.ForkSuffix);
        }     
    }
}
