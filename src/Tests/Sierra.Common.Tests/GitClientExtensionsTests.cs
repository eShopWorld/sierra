using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Sierra.Common;
using Sierra.Common.Tests;
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

    [Fact, IsIntegration]
    public async Task CreateForkIfNotExists_()
    {
        using (var scope = ContainerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<GitHttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var suffix = Guid.NewGuid().ToString();
            await sut.CreateForkIfNotExists(vstsConfig.VstsCollectionId, vstsConfig.VstsTargetProjectId, "ForkIntTestSourceRepo", suffix);
            var repo = (await sut.GetRepositoriesAsync()).FirstOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();
            await sut.DeleteRepositoryAsync(repo.Id);
        }
    }
}
