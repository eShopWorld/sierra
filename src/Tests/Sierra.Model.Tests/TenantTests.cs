using Eshopworld.Tests.Core;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Sierra.Model.Tests
{
    public class TenantTests
    {
        [Fact, IsUnit]
        public void Update_AddRepo()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName"
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] { new Fork { SourceRepositoryName = "A" } })
            };

            currentTenant.Update(tenantRequest);
            currentTenant.ForksToAdd.Count.Should().Be(1);
            currentTenant.ForksToAdd[0].SourceRepositoryName.Should().Be("A");
            currentTenant.ForksToAdd[0].TenantCode.Should().Be("TenantA");
            currentTenant.CustomSourceRepos.Count.Should().Be(1);
            currentTenant.CustomSourceRepos[0].SourceRepositoryName.Should().Be("A");
            currentTenant.CustomSourceRepos[0].TenantCode.Should().Be("TenantA");
            currentTenant.CustomSourceRepos[0].State.Should().Be(Fork.NotCreatedState);
        }

        [Fact, IsUnit]
        public void Update_AddAdditionalRepo()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] { new Fork { State=Fork.CreatedState, SourceRepositoryName="RepoA"} })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] { new Fork { SourceRepositoryName = "RepoB" } })
            };

            currentTenant.Update(tenantRequest);

            currentTenant.ForksToAdd.Count.Should().Be(1);
            currentTenant.ForksToAdd[0].SourceRepositoryName.Should().Be("");
            currentTenant.ForksToAdd[0].TenantCode.Should().Be("TenantA");
            currentTenant.CustomSourceRepos.Count.Should().Be(2);
            currentTenant.CustomSourceRepos[1].SourceRepositoryName.Should().Be("RepoB");
            currentTenant.CustomSourceRepos[1].TenantCode.Should().Be("TenantA");
            currentTenant.CustomSourceRepos[1].State.Should().Be(Fork.NotCreatedState);
        }

        [Fact, IsUnit]
        public void Update_RemRepo()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] 
                {
                    new Fork { State = Fork.CreatedState, SourceRepositoryName = "RepoA", TenantCode="TenantA" },
                    new Fork { State = Fork.CreatedState, SourceRepositoryName = "RepoB", TenantCode="TenantA" }
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] { new Fork { SourceRepositoryName = "RepoA" } })
            };

            currentTenant.Update(tenantRequest);

            currentTenant.ForksToRemove.Count.Should().Be(1);
            currentTenant.ForksToRemove[0].SourceRepositoryName.Should().Be("RepoB");
            currentTenant.ForksToRemove[0].TenantCode.Should().Be("TenantA");
        }
    }
}
