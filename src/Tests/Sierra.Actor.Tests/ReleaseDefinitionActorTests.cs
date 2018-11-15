using System;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Sierra.Common;
using Sierra.Model;
using Xunit;

[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class ReleaseDefinitionActorTests
{
    private ActorTestsFixture Fixture { get; }

    public ReleaseDefinitionActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;        
    }

    [Fact, IsLayer2]
    public async Task AddForkTest()
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var releaseClient = scope.Resolve<ReleaseHttpClient2>();

            var releaseDefinition = new VstsReleaseDefinition
            {
                BuildDefinition = new VstsBuildDefinition
                {
                    TenantCode = "L2TNT",
                    VstsBuildDefinitionId = vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    SourceCode = new SourceCodeRepository
                    {
                        TenantCode = "L2TNT",
                        ProjectType = ProjectTypeEnum.WebApi,
                        SourceRepositoryName = "ForkIntTestSourceRepo",
                        Fork = true                        
                    }
                },
                TenantCode = "L2TNT"
            };

            try
            {
                var resp = await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition);
                resp.State.Should().Be(EntityStateEnum.Created);
                resp.VstsReleaseDefinitionId.Should().NotBe(default);
                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                        resp.VstsReleaseDefinitionId);
                vstsRel.Should().NotBeNull();
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Remove",
                    releaseDefinition);
            }
        }
    }

    [Fact, IsLayer2]
    public async Task AddStandardTest()
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var releaseClient = scope.Resolve<ReleaseHttpClient2>();

            var releaseDefinition = new VstsReleaseDefinition
            {
                BuildDefinition = new VstsBuildDefinition
                {
                    TenantCode = "L2TNT",
                    VstsBuildDefinitionId = vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    SourceCode = new SourceCodeRepository
                    {
                        TenantCode = "L2TNT",
                        ProjectType = ProjectTypeEnum.WebApi,
                        SourceRepositoryName = "ForkIntTestSourceRepo",
                        Fork = false
                    }
                },
                TenantCode = "L2TNT",
                SkipEnvironments = new []{EnvironmentNames.PROD}
            };

            try
            {
                var resp = await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition);
                resp.State.Should().Be(EntityStateEnum.Created);
                resp.VstsReleaseDefinitionId.Should().NotBe(default);
                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    resp.VstsReleaseDefinitionId);
                vstsRel.Should().NotBeNull();
                vstsRel.Environments.Should().NotContain(e =>
                    e.Name.StartsWith(EnvironmentNames.PROD, StringComparison.OrdinalIgnoreCase)); //no PROD check
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Remove",
                    releaseDefinition);
            }
        }
    }

    [Fact, IsLayer2]
    public async Task RemoveForkTest()
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var releaseClient = scope.Resolve<ReleaseHttpClient2>();

            var releaseDefinition = new VstsReleaseDefinition
            {
                BuildDefinition = new VstsBuildDefinition
                {
                    TenantCode = "L2TNT",
                    VstsBuildDefinitionId = vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    SourceCode = new SourceCodeRepository
                    {
                        TenantCode = "L2TNT",
                        ProjectType = ProjectTypeEnum.WebApi,
                        SourceRepositoryName = "ForkIntTestSourceRepo",
                        Fork =  true
                    }
                },
                TenantCode = "L2TNT"
            };

            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition);                
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Remove",
                    releaseDefinition);

                (await releaseClient.GetReleaseDefinitionsAsync(vstsConfig.VstsTargetProjectId, releaseDefinition.ToString(),
                    isExactNameMatch: true)).Should().BeNullOrEmpty();
            }
        }
    }
}