namespace Sierra.Common
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Eshopworld.Telemetry;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Events;

    /// <summary>
    /// Contains Sierra extensions methods to the <see cref="GitHttpClient"/> part of the VSTS SDK.
    /// </summary>
    public static class GitHttpClientExtensions
    {
        /// <summary>
        /// Creates a repository Fork through the VSTS <see cref="GitHttpClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="GitHttpClient"/> used to create the Fork.</param>
        /// <param name="vstsCollectionId">The target collection ID where we are creating the fork on.</param>
        /// <param name="vstsTargetProjectId">The target project ID where we are creating the fork on.</param>
        /// <param name="sourceRepo">The origin repo for the Fork.</param>
        /// <param name="forkSuffix">The fork suffix that we want to give to the Fork name.</param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        internal static async Task CreateForkIfNotExists(this GitHttpClient client, string vstsCollectionId, string vstsTargetProjectId, GitRepository sourceRepo, string forkSuffix, BigBrother bigBrother)
        {
            var desiredName = $"{sourceRepo.Name}-{forkSuffix}";
            var repo = (await client.GetRepositoriesAsync()).SingleOrDefault(r => r.Name == desiredName);
            if (repo!=null)
            {
                if (!repo.IsFork || !repo.ProjectReference.Id.Equals(sourceRepo.ProjectReference.Id))
                    bigBrother.Publish(new ForkBbEvent {
                        ForkName = desiredName,
                        Message =$"Repository already exists but is not a fork or is a fork of another project"
                    });

                return;
            }

            await client.CreateRepositoryAsync(
                new GitRepositoryCreateOptions
                {
                    Name = desiredName,
                    ProjectReference = new TeamProjectReference {Id = Guid.Parse(vstsTargetProjectId)},
                    ParentRepository = new GitRepositoryRef
                    {
                        Id = sourceRepo.Id,
                        ProjectReference = new TeamProjectReference {Id = Guid.Parse(vstsTargetProjectId)},
                        Collection = new TeamProjectCollectionReference {Id = Guid.Parse(vstsCollectionId)}
                    }
                });

            bigBrother.Publish(new ForkBbEvent { ForkName = desiredName, Success = true });
        }
    }
}
