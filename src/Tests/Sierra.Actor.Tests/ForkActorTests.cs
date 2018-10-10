using System.Linq;
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
public class ForkActorTests
{
    private ActorTestsFixture Fixture { get; }
    private static readonly Fork TestFork = new Fork
    {
        TenantCode = "L2TNT",
        ProjectType = ProjectTypeEnum.WebApi,
        SourceRepositoryName = "ForkIntTestSourceRepo"
    };

    public ForkActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact, IsLayer2]
    public async Task AddTest()
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {           
            try
            {
                var forkCreated = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Fork", "Add", TestFork);
                
                var vstsClient = scope.Resolve<GitHttpClient>();
                var repo = await vstsClient.GetRepositoryAsync(forkCreated.ForkVstsId, true, null);
                repo.Should().NotBeNull();
                //check parent repo link                
                repo.ParentRepository.Name.Should().BeEquivalentTo(TestFork.SourceRepositoryName);
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Fork", "Remove", TestFork);                
            }
        }
    }

    [Fact, IsLayer2]
    public async Task RemoveTest()
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {          
            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Fork", "Add", TestFork);
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Fork", "Remove", TestFork);
                var vstsConfig = scope.Resolve<VstsConfiguration>();
                var vstsClient = scope.Resolve<GitHttpClient>();
                var repos = await vstsClient.GetRepositoriesAsync(vstsConfig.VstsTargetProjectId);
                repos.Should().NotContain(r => r.IsFork && r.Name == TestFork.ToString());
            }
        }
    }
}