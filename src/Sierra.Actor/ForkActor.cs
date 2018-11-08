using System;

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
    public class ForkActor : SierraActor<SourceCodeRepository>, IForkActor
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
        public override async Task<SourceCodeRepository> Add(SourceCodeRepository sourceCodeRepository)
        {
            //if fork not requested, look up the repo id
            if (!sourceCodeRepository.Fork)
            {
                var gitRepo = await _gitClient.LoadGitRepositoryIfExists(sourceCodeRepository.SourceRepositoryName);
                if (gitRepo == null)
                    throw new ArgumentException(
                        $"Repository {sourceCodeRepository.SourceRepositoryName} does not exist",
                        nameof(SourceCodeRepository));

                sourceCodeRepository.UpdateWithVstsRepo(gitRepo.Id);
                return sourceCodeRepository;
            }

            //otherwise fork to a new repo
            var repo = await _gitClient.CreateForkIfNotExists(_vstsConfiguration.VstsCollectionId, _vstsConfiguration.VstsTargetProjectId, sourceCodeRepository);

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
                sourceCodeRepository.UpdateWithVstsRepo(repo.Id);

                _bigBrother.Publish(new ForkRequestSucceeded { ForkName = repo.Name });
                return sourceCodeRepository;
            }

            return null;
        }

        /// <inheridoc/>
        public override async Task Remove(SourceCodeRepository sourceCodeRepository)
        {
            //do not delete standard component
            if (!sourceCodeRepository.Fork)
                return;

            var forkRemoved = await _gitClient.DeleteForkIfExists(sourceCodeRepository.ToString());

            if (forkRemoved)
            {
                _bigBrother.Publish(new ForkDeleted { ForkName = sourceCodeRepository.ToString() });
            }
        }
    }
}
