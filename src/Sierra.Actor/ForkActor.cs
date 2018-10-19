namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Common;
    using Common.Events;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Model;

    /// <summary>
    /// Manages Forks on behalf of tenant operations.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    public class ForkActor : SierraActor<Fork>, IForkActor
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
        /// <param name="bb">big brother instance</param>
        public ForkActor(ActorService actorService, ActorId actorId, GitHttpClient gitClient, VstsConfiguration vstsConfiguration, IBigBrother bb)
            : base(actorService, actorId)
        {
            _gitClient = gitClient;
            _vstsConfiguration = vstsConfiguration;
            _bigBrother = bb;
        }

        /// <inheridoc/>
        public override async Task<Fork> Add(Fork fork)
        {
            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, fork);

            if (!repo.IsFork)
            {
                _bigBrother.Publish(new ForkRequestFailed
                {
                    ForkName = repo.Name,
                    Message = $"Repository already exists but is not a fork"
                });
            }
            else
            {
                fork.UpdateWithVstsRepo(repo.Id);

                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
                return fork;
            }

            return null;
        }

        /// <inheridoc/>
        public override async Task Remove(Fork fork)
        {
            var forkRemoved = await _gitClient.DeleteForkIfExists(fork.ToString());

            if (forkRemoved)
            {
                _bigBrother.Publish(new ForkDeleted { ForkName = fork.ToString() });
            }
        }
    }
}
