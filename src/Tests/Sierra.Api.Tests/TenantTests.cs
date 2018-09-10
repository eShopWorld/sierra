namespace Sierra.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Autofac;
    using Common;
    using Eshopworld.Tests.Core;
    using FluentAssertions;
    using IdentityModel.Client;
    using Microsoft.TeamFoundation.Build.WebApi;
    using Model;
    using Xunit;

    [Collection(nameof(ActorContainerCollection))]
    public class TenantTests
    {
        private readonly ApiTestsFixture _containerFixture;
        private readonly string _tenantsControllerUrl;
        private readonly TestConfig _testConfig;
        private const string TenantName = "CITNT";
        private const int BuildWaitTimeout = 300; //seconds = 10 minutes

        public TenantTests(ApiTestsFixture containerFixture)
        {
            _containerFixture = containerFixture;
            _testConfig = _containerFixture.Container.Resolve<TestConfig>();
            _tenantsControllerUrl = _testConfig.ApiUrl + "tenants";
        }

        [Fact, IsLayer2]
        public async Task Tenant_Flow()
        {
            using (var scope = _containerFixture.Container.BeginLifetimeScope())
            {
                var dbContext = scope.Resolve<SierraDbContext>();
                try
                {
                    await EnsureTenantCreated();

                    var tenantRecord = await dbContext.LoadCompleteTenantAsync(TenantName);

                    tenantRecord.Should().NotBeNull();
                    tenantRecord.CustomSourceRepos.Should()
                        .ContainSingle(f =>
                            f.SourceRepositoryName == "ForkIntTestSourceRepo" && f.State == EntityStateEnum.Created &&
                            f.TenantCode == TenantName);
                    tenantRecord.BuildDefinitions.Should()
                        .ContainSingle(bd =>
                            bd.TenantCode == TenantName &&
                            bd.SourceCode.SourceRepositoryName == "ForkIntTestSourceRepo" &&
                            bd.State == EntityStateEnum.Created);
                }
                finally
                {
                    await EnsureTenantDeleted();

                    var tenantRecord = await dbContext.LoadCompleteTenantAsync(TenantName);
                    tenantRecord.Should().BeNull();
                    dbContext.Forks.Where(f => f.TenantCode == TenantName).Should().BeNullOrEmpty();
                    dbContext.BuildDefinitions.Where(bd => bd.TenantCode == TenantName).Should().BeNullOrEmpty();
                }
            }
        }

        [Fact, IsDev]
        public async Task Tenant_BuildDefinitionChecks()
        {
            try
            {
                await EnsureTenantCreated();

                using (var scope = _containerFixture.Container.BeginLifetimeScope())
                {
                    var dbContext = scope.Resolve<SierraDbContext>();
                    var vstsConfig = scope.Resolve<VstsConfiguration>();
                    var tenantRecord = await dbContext.LoadCompleteTenantAsync(TenantName);
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
                        .Wait(TimeSpan.FromSeconds(BuildWaitTimeout));

                    Assert.True(pollingCompleted, $"The polling for the build definition timed out after {BuildWaitTimeout} seconds");

                    build.Result.Should().Be(BuildResult.Succeeded);
                }
            }
            finally
            {
                await EnsureTenantDeleted();
            }
        }

        private async Task EnsureTenantCreated()
        {
            var newTenant = new Tenant
            {
                Code = TenantName,
                Name = $"Tenant{TenantName}",
                CustomSourceRepos =
                    new List<Fork>(new[] { new Fork { SourceRepositoryName = "ForkIntTestSourceRepo" } })
            };

            var client = new HttpClient();
            //obtain access token
            var stsAccessToken = await ObtainSTSAccessToken();
            client.SetBearerToken(stsAccessToken);

            //add tenant
            var resp = await client.PostAsJsonAsync(_tenantsControllerUrl, newTenant);
            resp.EnsureSuccessStatusCode();
        }

        private async Task EnsureTenantDeleted()
        {
            var client = new HttpClient();
            //obtain access token
            var stsAccessToken = await ObtainSTSAccessToken();
            client.SetBearerToken(stsAccessToken);

            var resp = await client.DeleteAsync($"{_tenantsControllerUrl}/{TenantName}");
            resp.EnsureSuccessStatusCode();
        }

        // ReSharper disable once InconsistentNaming
        private async Task<string> ObtainSTSAccessToken()
        {
            var discovery = await DiscoveryClient.GetAsync(_testConfig.STSAuthority);
            var client = new TokenClient(discovery.TokenEndpoint, _testConfig.STSClientId, _testConfig.STSClientSecret);

            var tokenResponse = await client.RequestClientCredentialsAsync(_testConfig.STSScope);
            return tokenResponse.AccessToken;
        }
    }
}