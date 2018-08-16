using Eshopworld.Tests.Core;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Sierra.Model.Tests
{
    public class TenantTests
    {
        [Fact, IsUnit]
        public void Update_AddAdditionalRepo()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new [] { new Fork { SourceRepositoryName="AlreadyThere", State=Fork.CreatedState, TenantCode="TenantA"} })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] {
                    new Fork { SourceRepositoryName = "A" },
                    new Fork { SourceRepositoryName = "AlreadyThere" }
                })
            };

            currentTenant.Update(tenantRequest);
            currentTenant.ForksToAdd.Should().ContainSingle(f => f.SourceRepositoryName == "A" && f.TenantCode == "TenantA");
         
            currentTenant.CustomSourceRepos.Count.Should().Be(2);
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "AlreadyThere" && f.State == Fork.CreatedState && f.TenantCode == "TenantA");
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "A" && f.State == Fork.NotCreatedState && f.TenantCode == "TenantA");
        }

        [Fact, IsUnit]
        public void Update_AddRepo()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>()
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] { new Fork { SourceRepositoryName = "RepoB" } })
            };

            currentTenant.Update(tenantRequest);

            currentTenant.ForksToAdd.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA");
  
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" && f.State==Fork.NotCreatedState);            
        }

        [Fact, IsUnit]
        public void Update_RemoveRepo()
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

            currentTenant.ForksToRemove.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA");            
        }
    }
}
