namespace Sierra.Common
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Model;

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
            var repo = await client.LoadGitRepositoryIfExists(targetRepo);

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
        /// <param name="sourceCodeRepository">fork definition</param>
        /// <returns>The async <see cref="Task{GitRepository}"/> wrapper with pre-existing or new repo</returns>
        internal static async Task<GitRepository> CreateForkIfNotExists(this GitHttpClient client, string vstsCollectionId, string vstsTargetProjectId, SourceCodeRepository sourceCodeRepository)
        {
            var sourceRepo = await client.LoadGitRepositoryIfExists(sourceCodeRepository.SourceRepositoryName);

            if (sourceRepo == null)
                throw new ArgumentException($"Repository {sourceCodeRepository.SourceRepositoryName} not found");

            return await CreateForkIfNotExists(client, vstsCollectionId, vstsTargetProjectId, sourceRepo, sourceCodeRepository.ToString());
        }

        /// <summary>
        /// delete fork if exists
        /// 
        /// if repo does not exist, just return gracefully
        /// </summary>
        /// <param name="client">extension entry-point</param>
        /// <param name="forkName">fork definition</param>
        /// <returns>fork deletion result</returns>
        internal static async Task<bool> DeleteForkIfExists(this GitHttpClient client, string forkName)
        {
            var repo = await client.LoadGitRepositoryIfExists(forkName);

            if (repo == null)
                return false;

            await client.DeleteRepositoryAsync(repo.Id);

            return true;
        }

        /// <summary>
        /// load repo definition if it exists
        /// </summary>
        /// <param name="client">extension method entry-point</param>
        /// <param name="name">name of the repo to locate</param>
        /// <returns>repo definition if it exists</returns>
        internal static async Task<GitRepository> LoadGitRepositoryIfExists(this GitHttpClient client, string name)
        {
            return (await client.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == name);
        }
    }
}
