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
        public async Task Post_NewTenant()
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
            var resp = await client.PostAsJsonAsync(TenantsControllerUrl, newTenant);
            resp.EnsureSuccessStatusCode();
            using (var dbContext = ContainerFixture.Container.Resolve<SierraDbContext>())
            {
                var tenantRecord = await dbContext.LoadCompleteTenantAsync("ABCDEF");
                tenantRecord.Should().NotBeNull();
                tenantRecord.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "ForkIntTestSourceRepo" && f.State == ForkState.Created && f.TenantCode == "ABCDEF");
            }
        }
    }
}
