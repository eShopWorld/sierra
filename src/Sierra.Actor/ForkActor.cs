namespace Sierra.Actor
{
    using System.Linq;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using Common;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Common.Events;
    using Eshopworld.Core;
    using System.Collections.Generic;

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

        /// <inheridoc/>
        public async Task Add(Fork fork)
        {
            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, fork);

            if (!repo.IsFork)
                _bigBrother.Publish(new ForkRequestFailed
                {
                    ForkName = repo.Name,
                    Message = $"Repository already exists but is not a fork"
                });
            else         
                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
        }

        /// <inheridoc/>
        public async Task Remove(string fork)
        {           
            var forkRemoved = await _gitClient.DeleteForkIfExists(fork);

            if (forkRemoved)
                _bigBrother.Publish(new ForkDeleted { ForkName = fork });
            
        }

        /// <inheridoc/>
        public async Task<List<string>> QueryTenantRepos(string tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
                return null;

            return
                (await _gitClient.GetRepositoriesAsync())
                .Where(r => r.IsFork && r.Name.EndsWith(tenantName))
                .Select(t => t.Name).ToList();            
        }
    }
}
