namespace Sierra.Common
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Sierra.Model;

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
        /// <param name="targetRepo">name of the tenant fork</param>
        /// <returns>The async <see cref="Task{GitRepository}"/> wrapper with pre-existing or new repo</returns>
        internal static async Task<GitRepository> CreateForkIfNotExists(this GitHttpClient client, string vstsCollectionId, string vstsTargetProjectId, GitRepository sourceRepo, string targetRepo)
        {
            var repo = (await client.GetRepositoriesAsync()).SingleOrDefault(r => r.Name == targetRepo);

            if (repo != null)
                return repo;

            return await client.CreateRepositoryAsync(
                new GitRepositoryCreateOptions
                {
                    Name = targetRepo,
                    ProjectReference = new TeamProjectReference { Id = Guid.Parse(vstsTargetProjectId) },
                    ParentRepository = new GitRepositoryRef
                    {
                        Id = sourceRepo.Id,
                        ProjectReference = new TeamProjectReference { Id = sourceRepo.ProjectReference.Id },
                        Collection = new TeamProjectCollectionReference { Id = Guid.Parse(vstsCollectionId) }
                    }
                });
        }

        /// <summary>
        /// Creates a repository Fork through the VSTS <see cref="GitHttpClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="GitHttpClient"/> used to create the Fork.</param>
        /// <param name="vstsCollectionId">The target collection ID where we are creating the fork on.</param>
        /// <param name="vstsTargetProjectId">The target project ID where we are creating the fork on.</param>
        /// <param name="fork">fork definition</param>
        /// <returns>The async <see cref="Task{GitRepository}"/> wrapper with pre-existing or new repo</returns>
        internal static async Task<GitRepository> CreateForkIfNotExists(this GitHttpClient client, string vstsCollectionId, string vstsTargetProjectId, Fork fork)
        {            
            var sourceRepo = (await client.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == fork.SourceRepositoryName);

            if (sourceRepo == null)
                throw new ArgumentException($"Repository {fork.SourceRepositoryName} not found");

            return await CreateForkIfNotExists(client, vstsCollectionId, vstsTargetProjectId, sourceRepo, fork.ToString());
        }

        /// <summary>
        /// delete fork if exists
        /// 
        /// if repo does not exist, just return gracefully
        /// </summary>
        /// <param name="fork">fork definition</param>
        /// <returns>fork deletion result</returns>
        internal static async Task<bool> DeleteForkIfExists(this GitHttpClient client, string forkName)
        {
            if (string.IsNullOrWhiteSpace(forkName))
                return false;
         
            var repo = (await client.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == forkName);

            if (repo == null)
                return false;

            await client.DeleteRepositoryAsync(repo.Id);

            return true;
        }           
    }        
}
