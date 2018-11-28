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
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
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
                resp.VstsReleaseDefinitionId.Should().NotBe(default(int));
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
    public async Task AddStandardNonRingTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
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
                SkipEnvironments = new []{DeploymentEnvironment.Prod},
                RingBased = false
            };

            try
            {
                var resp = await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition);
                resp.State.Should().Be(EntityStateEnum.Created);
                resp.VstsReleaseDefinitionId.Should().NotBe(default(int));
                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    resp.VstsReleaseDefinitionId);
                vstsRel.Should().NotBeNull();
                vstsRel.Environments.Should().NotContain(e =>
                    e.Name.StartsWith(DeploymentEnvironment.Prod.ToString(), StringComparison.OrdinalIgnoreCase)); //no PROD check
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
    public async Task AddStandardRingTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
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
                TenantSize =  TenantSize.Small,
                RingBased = true
            };

            try
            {
                var resp = await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition);
                resp.State.Should().Be(EntityStateEnum.Created);
                resp.VstsReleaseDefinitionId.Should().NotBe(default(int));
                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    resp.VstsReleaseDefinitionId);
                vstsRel.Should().NotBeNull(); 
                //check variable containing definition
                vstsRel.Variables["SmallTenants"].Value.Should().Be("L2TNT#11111");
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
    public async Task MultipleTenantsToRingTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var releaseClient = scope.Resolve<ReleaseHttpClient2>();

            var releaseDefinition1 = new VstsReleaseDefinition
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
                TenantSize = TenantSize.Small,
                RingBased = true
            };

            var releaseDefinition2 = new VstsReleaseDefinition
            {
                BuildDefinition = new VstsBuildDefinition
                {
                    TenantCode = "L2TNT2",
                    VstsBuildDefinitionId = vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    SourceCode = new SourceCodeRepository
                    {
                        TenantCode = "L2TNT2",
                        ProjectType = ProjectTypeEnum.WebApi,
                        SourceRepositoryName = "ForkIntTestSourceRepo",
                        Fork = false
                    }
                },
                TenantCode = "L2TNT2",
                TenantSize = TenantSize.Small,
                RingBased = true
            };

            var pipelineId=0;
            try
            {
                //first tenant
                var resp1 = await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Add",
                    releaseDefinition1);
                resp1.State.Should().Be(EntityStateEnum.Created);
                resp1.VstsReleaseDefinitionId.Should().NotBe(default(int));

                //second tenant
                var resp2 = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ReleaseDefinition", "Add",
                    releaseDefinition2);
                resp2.State.Should().Be(EntityStateEnum.Created);
                resp2.VstsReleaseDefinitionId.Should().NotBe(default(int));

                //checks
                resp1.VstsReleaseDefinitionId.Should().Be(resp2.VstsReleaseDefinitionId); //sharing the ring

                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    resp1.VstsReleaseDefinitionId);
                vstsRel.Should().NotBeNull();
                //check variable containing definition
                vstsRel.Variables["SmallTenants"].Value.Should().ContainAll("L2TNT2#11111", "L2TNT#11111");
                pipelineId = resp1.VstsReleaseDefinitionId;
            }         
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ReleaseDefinition", "Remove", releaseDefinition1);
                //check tenant definition gone

                var vstsRel = await releaseClient.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId, pipelineId);
                vstsRel.Variables["SmallTenants"].Value.Should().Be("L2TNT2#11111");

                await cl.PostJsonToActor(Fixture.TestMiddlewareUri,
                    "ReleaseDefinition", "Remove",
                    releaseDefinition2);
                //check pipeline gone
                var pipelines = await releaseClient.GetReleaseDefinitionsAsync2(vstsConfig.VstsTargetProjectId);
                pipelines.Should().NotContain(p => p.Id == pipelineId);
            }
        }
    }

    [Fact, IsLayer2]
    public async Task RemoveForkTest()
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
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