using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Sierra.Common;
using Sierra.Model;
using Xunit;

[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class RepositoryActorTests
{
    private ActorTestsFixture Fixture { get; }
    private static readonly SourceCodeRepository TestFork = new SourceCodeRepository
    {
        TenantCode = "L2TNT",
        ProjectType = ProjectTypeEnum.WebApi,
        SourceRepositoryName = "ForkIntTestSourceRepo",
        Fork = true
    };

    private static readonly SourceCodeRepository TestStandard = new SourceCodeRepository
    {
        TenantCode = "L2TNT",
        ProjectType = ProjectTypeEnum.WebApi,
        SourceRepositoryName = "ForkIntTestSourceRepo",
        Fork = false
    };

    private const string ActorName = "Repository";

    public RepositoryActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact, IsLayer2]
    public async Task AddForkTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            try
            {
                var forkCreated = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Add", TestFork);

                var vstsClient = scope.Resolve<GitHttpClient>();
                var repo = await vstsClient.GetRepositoryAsync(forkCreated.RepoVstsId, true, null);
                repo.Should().NotBeNull();
                //check parent repo link                
                repo.ParentRepository.Name.Should().BeEquivalentTo(TestFork.SourceRepositoryName);
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Remove", TestFork);
            }
        }
    }

    [Fact, IsLayer2]
    public async Task AddStandardTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Add", TestStandard);
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var vstsClient = scope.Resolve<GitHttpClient>();
            var repos = await vstsClient.GetRepositoriesAsync(vstsConfig.VstsTargetProjectId, includeHidden: true);
            //check no fork was created
            repos.Should().NotContain(r => r.Name == TestFork.ToString());
        }
    }

    [Fact, IsLayer2]
    public async Task StandardRepoNotRemovedTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Remove", TestStandard);
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var vstsClient = scope.Resolve<GitHttpClient>();
            var repos = await vstsClient.GetRepositoriesAsync(vstsConfig.VstsTargetProjectId, includeHidden: true);
            //check standard repo still there
            repos.Should().ContainSingle(r => r.Name == TestStandard.ToString());
        }
    }

    [Fact, IsLayer2]
    public async Task RemoveTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Add", TestFork);
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, ActorName, "Remove", TestFork);
                var vstsConfig = scope.Resolve<VstsConfiguration>();
                var vstsClient = scope.Resolve<GitHttpClient>();
                var repos = await vstsClient.GetRepositoriesAsync(vstsConfig.VstsTargetProjectId);
                repos.Should().NotContain(r => r.IsFork && r.Name == TestFork.ToString());
            }
        }
    }
}