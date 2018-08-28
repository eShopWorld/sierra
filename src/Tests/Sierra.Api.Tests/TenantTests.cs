using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using Microsoft.Extensions.Configuration;
using Sierra.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using IdentityModel.Client;
using System.Linq;

namespace Sierra.Api.Tests
{
    [Collection(nameof(ActorContainerCollection))]
    public class TenantTests
    {
        public readonly ActorContainerFixture ContainerFixture;

        public readonly TestConfig Config = new TestConfig();

        public readonly string TenantsControllerUrl;
        public TenantTests(ActorContainerFixture containerFixture)
        {
            ContainerFixture = containerFixture;
            var config = EswDevOpsSdk.BuildConfiguration(true);
            config.GetSection("TestConfig").Bind(Config);
            TenantsControllerUrl = Config.ApiUrl + "tenants";
        }

        [Fact, IsLayer2]
        public async Task TenantFlow()
        {
            var newTenant = new Tenant
            {
                Code = "ABCDEF",
                Name = "TenantABCDEF",
                CustomSourceRepos = new List<Fork>(new[]
                {
                    new Fork { SourceRepositoryName="ForkIntTestSourceRepo"}
                })
            };

            HttpClient client = new HttpClient();
            //obtain access token
            var stsAccessToken = await ObtainSTSAccessToken();
            client.SetBearerToken(stsAccessToken);
            //add tenant
            var resp = await client.PostAsJsonAsync(TenantsControllerUrl, newTenant);
            resp.EnsureSuccessStatusCode();
            using (var dbContext = ContainerFixture.Container.Resolve<SierraDbContext>())
            {
                var tenantRecord = await dbContext.LoadCompleteTenantAsync("ABCDEF");
                tenantRecord.Should().NotBeNull();
                tenantRecord.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "ForkIntTestSourceRepo" && f.State == EntityStateEnum.Created && f.TenantCode == "ABCDEF");
            }

            //remove tenant
            resp = await client.DeleteAsync(TenantsControllerUrl + "/ABCDEF");
            resp.EnsureSuccessStatusCode();
            using (var dbContext = ContainerFixture.Container.Resolve<SierraDbContext>())
            {
                var tenantRecord = await dbContext.LoadCompleteTenantAsync("ABCDEF");
                tenantRecord.Should().BeNull();
                dbContext.Forks.Where(f => f.TenantCode == "ABCDEF").Should().BeNullOrEmpty();
            }
        }

        // ReSharper disable once InconsistentNaming
        private async Task<string> ObtainSTSAccessToken()
        {
            var discovery = await DiscoveryClient.GetAsync(Config.STSAuthority);
            var client = new TokenClient(discovery.TokenEndpoint, Config.STSClientId, Config.STSClientSecret);

            var tokenResponse = await client.RequestClientCredentialsAsync(Config.STSScope);
            return tokenResponse.AccessToken;
        }
    }
}
