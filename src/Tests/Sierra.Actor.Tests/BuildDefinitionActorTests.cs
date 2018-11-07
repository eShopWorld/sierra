using System;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.Build.WebApi;
using Sierra.Common;
using Sierra.Model;
using Xunit;


[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class BuildDefinitionActorTests
{
    private ActorTestsFixture Fixture { get; }

    private static readonly VstsBuildDefinition TestBuildDefinition = new VstsBuildDefinition(new SourceCodeRepository
    {
        TenantCode = "L2TNT",
        ProjectType = ProjectTypeEnum.WebApi,
        SourceRepositoryName = "ForkIntTestSourceRepo",
        RepoVstsId = Guid.Parse("2c71775d-25a9-4e16-b880-9880cc8b9f1c")
    }, "L2TNT");
    

    public BuildDefinitionActorTests(ActorTestsFixture fixture)
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
                VstsBuildDefinition resp = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "BuildDefinition", "Add", TestBuildDefinition);                
                
                var vstsConfig = scope.Resolve<VstsConfiguration>();
                var buildClient = scope.Resolve<BuildHttpClient>();

                var buildDefinition = await 
                    buildClient.GetDefinitionAsync(vstsConfig.VstsTargetProjectId, resp.VstsBuildDefinitionId);

                buildDefinition.Should().NotBeNull();
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "BuildDefinition", "Remove", TestBuildDefinition);
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
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "BuildDefinition", "Add", TestBuildDefinition);
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "BuildDefinition", "Remove", TestBuildDefinition);
                var vstsConfig = scope.Resolve<VstsConfiguration>();
                var buildClient = scope.Resolve<BuildHttpClient>();

                var buildDefinition = await
                    buildClient.GetDefinitionsAsync2(project: vstsConfig.VstsTargetProjectId);

                buildDefinition.Should().NotContain(d => d.Name== TestBuildDefinition.ToString());
            }
        }

    }
}