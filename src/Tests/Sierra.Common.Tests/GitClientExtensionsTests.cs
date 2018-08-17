using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Sierra.Common;
using Sierra.Common.Tests;
using Sierra.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

[Collection(nameof(CommonContainerCollection))]
public class GitClientExtensionsTests
{
    public readonly CommonContainerFixture ContainerFixture;

    public GitClientExtensionsTests(CommonContainerFixture container)
    {
        ContainerFixture = container;
    }

    [Fact, IsLayer1]
    public async Task CreateForkIfNotExists_SuccessFlow()
    {
        using (var scope = ContainerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var suffix = Guid.NewGuid().ToString();
            //create test repo
            var newFork = new Fork("ForkIntTestSourceRepo", suffix);
            await sut.CreateForkIfNotExists(vstsConfig.VstsCollectionId, vstsConfig.VstsTargetProjectId, newFork);
            var repo = (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();
            //clean up
            await sut.DeleteRepositoryAsync(repo.Id);
        }
    }

    [Fact, IsLayer1]
    public async Task DeleteForkIfExists_SuccessFlow()
    {
        using (var scope = ContainerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var suffix = Guid.NewGuid().ToString();
            var fork = new Fork("ForkIntTestSourceRepo", suffix);
            //create target repo
            await sut.CreateForkIfNotExists(vstsConfig.VstsCollectionId, vstsConfig.VstsTargetProjectId, fork);
            var repo = (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();
            //delete target repo
            await sut.DeleteForkIfExists($"ForkIntTestSourceRepo-{suffix}");
            (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}").Should().BeNull();
        }
    }
}
