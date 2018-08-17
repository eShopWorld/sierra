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
                CustomSourceRepos = new List<Fork>(new [] { new Fork { SourceRepositoryName="AlreadyThere", State=ForkState.Created, TenantCode="TenantA"} })
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
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "AlreadyThere" && f.State == ForkState.Created && f.TenantCode == "TenantA");
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "A" && f.State == ForkState.NotCreated && f.TenantCode == "TenantA");
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
  
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" && f.State==ForkState.NotCreated);            
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
                    new Fork { State = ForkState.Created, SourceRepositoryName = "RepoA", TenantCode="TenantA" },
                    new Fork { State = ForkState.Created, SourceRepositoryName = "RepoB", TenantCode="TenantA" }
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
