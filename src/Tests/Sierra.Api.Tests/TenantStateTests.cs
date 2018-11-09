using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.TeamFoundation.Build.WebApi;
using Sierra.Common;
using Sierra.Model;
using Xunit;

[Collection(nameof(TenantL3Collection))]
// ReSharper disable once CheckNamespace
public class TenantStateTests
{
    private readonly TenantL3TestFixture _level3Fixture;

    private static readonly TimeSpan BuildWaitTimeout = TimeSpan.FromSeconds(300); 

    public TenantStateTests(TenantL3TestFixture level3Fixture)
    {
        _level3Fixture = level3Fixture;
    }

    [Fact, IsLayer3]
    public void CheckForkState()
    {    
            var tenantRecord =  _level3Fixture.TenantUnderTest;
            tenantRecord.Should().NotBeNull();

            tenantRecord.SourceRepos.Should()
                .ContainSingle(f =>
                    f.SourceRepositoryName == _level3Fixture.ForkSourceRepo && f.State == EntityStateEnum.Created &&
                    f.TenantCode == _level3Fixture.TenantCode);            
    }

    [Fact, IsLayer3]
    public void CheckBuildDefinitionState()
    {
        var tenantRecord = _level3Fixture.TenantUnderTest;
        tenantRecord.Should().NotBeNull();

        tenantRecord.BuildDefinitions.Should()
            .ContainSingle(bd =>
                bd.TenantCode == _level3Fixture.TenantCode && bd.SourceCode.SourceRepositoryName == _level3Fixture.ForkSourceRepo &&
                bd.State == EntityStateEnum.Created);
    }

    [Fact, IsLayer3]
    public void CheckReleaseDefinitionState()
    {
        var tenantRecord = _level3Fixture.TenantUnderTest;
        tenantRecord.Should().NotBeNull();

        tenantRecord.ReleaseDefinitions.Should()
            .ContainSingle(rd =>
                rd.TenantCode == _level3Fixture.TenantCode && rd.BuildDefinition!=null && rd.State == EntityStateEnum.Created);
    }

    [Fact, IsDev]
    public async Task Tenant_BuildDefinitionChecks()
    {
        using (var scope = _level3Fixture.Container.BeginLifetimeScope())
        {
            var dbContext = scope.Resolve<SierraDbContext>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            var tenantRecord = await dbContext.LoadCompleteTenantAsync(_level3Fixture.TenantCode);
            var bdRecord = tenantRecord.BuildDefinitions.First();

            var buildHttpClient = scope.Resolve<BuildHttpClient>();
            var build = await buildHttpClient.QueueBuildAsync(
                new Build {Definition = new DefinitionReference {Id = bdRecord.VstsBuildDefinitionId}},
                vstsConfig.VstsTargetProjectId);

            //poll for the build completion
            var pollingCompleted = Task.Run(async () =>
                {
                    do
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        build = await buildHttpClient.GetBuildAsync(vstsConfig.VstsTargetProjectId, build.Id);
                    } while (build.Status != BuildStatus.Completed);
                })
                .Wait(BuildWaitTimeout);

            Assert.True(pollingCompleted,
                $"The polling for the build definition timed out after {BuildWaitTimeout} seconds");

            build.Result.Should().Be(BuildResult.Succeeded);
        }
    }
}