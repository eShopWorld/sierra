using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Sierra.Common;
using Sierra.Common.Tests;
using Sierra.Model;
using Xunit;

[Collection(nameof(CommonContainerCollection))]
// ReSharper disable once CheckNamespace
public class GitHttpClientExtensionsTests
{
    private readonly CommonContainerFixture _containerFixture;

    public GitHttpClientExtensionsTests(CommonContainerFixture container)
    {
        _containerFixture = container;
    }

    [Fact, IsLayer1]
    public async Task CreateForkIfNotExists_SuccessFlow()
    {
        using (var scope = _containerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var suffix = Guid.NewGuid().ToString();
            //create test repo
            var newFork = new SourceCodeRepository("ForkIntTestSourceRepo", suffix, ProjectTypeEnum.WebApi, true);
            await sut.CreateForkIfNotExists(vstsConfig.VstsCollectionId, vstsConfig.VstsTargetProjectId, newFork);
            var repo = (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();
            //clean up
            // ReSharper disable once PossibleNullReferenceException
            await sut.DeleteRepositoryAsync(repo.Id);
        }
    }

    [Fact, IsLayer1]
    public async Task DeleteForkIfExists_SuccessFlow()
    {
        using (var scope = _containerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var suffix = Guid.NewGuid().ToString();
            var fork = new SourceCodeRepository("ForkIntTestSourceRepo", suffix, ProjectTypeEnum.WebApi, true);
            //create target repo
            await sut.CreateForkIfNotExists(vstsConfig.VstsCollectionId, vstsConfig.VstsTargetProjectId, fork);
            var repo = (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();
            //delete target repo
            await sut.DeleteForkIfExists($"ForkIntTestSourceRepo-{suffix}");
            (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}").Should().BeNull();
        }
    }

    [Fact, IsLayer1]
    public async Task LoadGitRepositoryIfExists()
    {
        using (var scope = _containerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();

            var repo = await sut.LoadGitRepositoryIfExists("ForkIntTestSourceRepo");
            repo.Id.Should().NotBe(default(Guid));
        }
    }
}
