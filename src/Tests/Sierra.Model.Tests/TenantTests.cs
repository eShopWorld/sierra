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
                CustomSourceRepos = new List<Fork>(new [] { new Fork { SourceRepositoryName="AlreadyThere", State=EntityStateEnum.Created, TenantCode="TenantA"} })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] {
                    new Fork { SourceRepositoryName = "A" },
                    new Fork { SourceRepositoryName = "AlreadyThere" }
                })
            };

            currentTenant.Update(tenantRequest);

            //forks checks
            currentTenant.ForksToAdd.Should().ContainSingle(f => f.SourceRepositoryName == "A" && f.TenantCode == "TenantA");         
            currentTenant.CustomSourceRepos.Count.Should().Be(2);
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "AlreadyThere" && f.State == EntityStateEnum.Created && f.TenantCode == "TenantA");
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "A" && f.State == EntityStateEnum.NotCreated && f.TenantCode == "TenantA");
            //build definition checks
            currentTenant.BuildDefinitionsToAdd.Should().ContainSingle(d => d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" && d.SourceCode.SourceRepositoryName == "A");
            currentTenant.BuildDefinitionsToRemove.Should().BeEmpty();
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

            //fork checks
            currentTenant.ForksToAdd.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA");  
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" && f.State==EntityStateEnum.NotCreated);
            //build definition checks
            currentTenant.BuildDefinitionsToAdd.Should().ContainSingle(d => d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" && d.SourceCode.SourceRepositoryName == "RepoB");
            currentTenant.BuildDefinitionsToRemove.Should().BeEmpty();
        }

        [Fact, IsUnit]
        public void Update_CheckNotCreatedForkRetry()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new [] {
                    new Fork { SourceRepositoryName="RepoA", State = EntityStateEnum.NotCreated, TenantCode = "TenantA"}
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] {
                    new Fork { SourceRepositoryName = "RepoA" },
                    new Fork { SourceRepositoryName = "RepoB" }
                })
            };

            currentTenant.Update(tenantRequest);

            currentTenant.ForksToAdd.Should().OnlyContain(f => (f.SourceRepositoryName == "RepoA" || f.SourceRepositoryName == "RepoB") && f.TenantCode == "TenantA");
            currentTenant.ForksToRemove.Should().BeEmpty();
        }

        [Fact, IsUnit]
        public void Update_CheckToBeDeletedForkRetry()
        {
            var forkRepoA = new Fork { SourceRepositoryName = "RepoA", State = EntityStateEnum.ToBeDeleted, TenantCode = "TenantA" };
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] {
                    forkRepoA
                }),
                BuildDefinitions = new List<BuildDefinition>(new[]
                {
                    new BuildDefinition {SourceCode = forkRepoA, State = EntityStateEnum.Created, TenantCode = "TenantA"}
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] {
                    new Fork { SourceRepositoryName = "RepoA" },
                    new Fork { SourceRepositoryName = "RepoB" }
                })
            };

            currentTenant.Update(tenantRequest);

            currentTenant.ForksToRemove.Should().ContainSingle(f => f.SourceRepositoryName == "RepoA" && f.TenantCode == "TenantA");
        }

        [Fact, IsUnit]
        public void Update_CheckNotCreatedBuildDefinitionRetry()
        {
            var forkRepoA = new Fork { SourceRepositoryName = "RepoA", State = EntityStateEnum.Created, TenantCode = "TenantA" };
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] {
                    forkRepoA
                }),
                BuildDefinitions = new List<BuildDefinition>(new[]
                {
                    new BuildDefinition {SourceCode = forkRepoA, State = EntityStateEnum.NotCreated, TenantCode = "TenantA"}
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] {
                    new Fork { SourceRepositoryName = "RepoA" }                   
                })
            };
            currentTenant.Update(tenantRequest);

            currentTenant.BuildDefinitionsToAdd.Should().ContainSingle(d => d.SourceCode == forkRepoA && d.TenantCode == "TenantA");
        }

        [Fact, IsUnit]
        public void Update_CheckToBeDeletedBuildDefinitionRetry()
        {
            var forkRepoA = new Fork { SourceRepositoryName = "RepoA", State = EntityStateEnum.Created, TenantCode = "TenantA" };
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] {
                    forkRepoA
                }),
                BuildDefinitions = new List<BuildDefinition>(new[]
                {
                    new BuildDefinition {SourceCode = forkRepoA, State = EntityStateEnum.ToBeDeleted, TenantCode = "TenantA"}
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>()
            };
            currentTenant.Update(tenantRequest);

            currentTenant.BuildDefinitionsToRemove.Should().ContainSingle(d => d.SourceCode == forkRepoA && d.TenantCode == "TenantA");
        }

        [Fact, IsUnit]
        public void Update_RemoveRepo()
        {
            var forkRepoA = new Fork { State = EntityStateEnum.Created, SourceRepositoryName = "RepoA", TenantCode = "TenantA" };
            var forkRepoB = new Fork { State = EntityStateEnum.Created, SourceRepositoryName = "RepoB", TenantCode = "TenantA" };
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] 
                {
                    forkRepoA, forkRepoB                   
                }),
                BuildDefinitions = new List<BuildDefinition>(new []
                {
                    new BuildDefinition{SourceCode= forkRepoA, TenantCode = "TenantA", State = EntityStateEnum.Created },
                    new BuildDefinition { SourceCode = forkRepoB, TenantCode = "TenantA", State = EntityStateEnum.Created }
                })
            };

            var tenantRequest = new Tenant
            {
                CustomSourceRepos = new List<Fork>(new[] { new Fork { SourceRepositoryName = "RepoA" } })
            };

            currentTenant.Update(tenantRequest);

            //forks checks
            currentTenant.ForksToAdd.Should().BeEmpty();
            currentTenant.ForksToRemove.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA");
            //build definition checks
            currentTenant.BuildDefinitionsToAdd.Should().BeEmpty();
            currentTenant.BuildDefinitionsToRemove.Should().ContainSingle(d => d.SourceCode.SourceRepositoryName == "RepoB" && forkRepoA.TenantCode == "TenantA");
        }       
    }
}
