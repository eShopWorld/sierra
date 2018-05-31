using System;
using System.Collections.Generic;
using System.Text;

namespace Sierra.Common
{
    using System.Threading.Tasks;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;

    public static class GitHttpClientExtensions
    {
        internal static async Task CreateFork(this GitHttpClient client, string vstsCollectionId, string vstsTargetProjectId, GitRepository sourceRepo, string forkSuffix)
        {
            // TODO: MISSING IDEMPOTENCY CHECK
            await client.CreateRepositoryAsync(
                new GitRepositoryCreateOptions
                {
                    Name = $"{sourceRepo.Name}-{forkSuffix}",
                    ProjectReference = new TeamProjectReference {Id = Guid.Parse(vstsTargetProjectId)},
                    ParentRepository = new GitRepositoryRef
                    {
                        Id = sourceRepo.Id,
                        ProjectReference = new TeamProjectReference {Id = Guid.Parse(vstsTargetProjectId)},
                        Collection = new TeamProjectCollectionReference {Id = Guid.Parse(vstsCollectionId)}
                    }
                });
        }
    }
}
