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
            var fork = new Fork { SourceRepositoryName = "AlreadyThere", State = EntityStateEnum.Created, TenantCode = "TenantA" };
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] { fork }),
                BuildDefinitions = new List<BuildDefinition>(new[] { new BuildDefinition { SourceCode = fork, TenantCode = "TenantA", State = EntityStateEnum.Created } })
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
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.State == EntityStateEnum.NotCreated);
            currentTenant.CustomSourceRepos.Count.Should().Be(2);
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "A" && f.TenantCode == "TenantA" && f.State == EntityStateEnum.NotCreated);           
            currentTenant.CustomSourceRepos.Should().Contain(f => f.SourceRepositoryName == "AlreadyThere" && f.State == EntityStateEnum.Created && f.TenantCode == "TenantA");
            //build definition checks
            currentTenant.CustomSourceRepos.Count.Should().Be(2);
            currentTenant.BuildDefinitions.Should().ContainSingle(bd => bd.State == EntityStateEnum.NotCreated);
            currentTenant.BuildDefinitions.Should().ContainSingle(d => d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" && d.SourceCode.SourceRepositoryName == "A");            
            currentTenant.BuildDefinitions.Should().NotContain(bd => bd.State == EntityStateEnum.ToBeDeleted);
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
            currentTenant.CustomSourceRepos.Should().OnlyContain(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" && f.State == EntityStateEnum.NotCreated);
            //build definition checks       
            currentTenant.BuildDefinitions.Should().OnlyContain(d => d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" && d.SourceCode.SourceRepositoryName == "RepoB");            
        }

        [Fact, IsUnit]
        public void Update_CheckNotCreatedForkRetry()
        {
            var currentTenant = new Tenant
            {
                Code = "TenantA",
                Name = "oldName",
                CustomSourceRepos = new List<Fork>(new[] {
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

            currentTenant.CustomSourceRepos.Should().OnlyContain(f => (f.SourceRepositoryName == "RepoA" || f.SourceRepositoryName == "RepoB") && f.TenantCode == "TenantA");
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

            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "RepoA" && f.TenantCode == "TenantA" && f.State == EntityStateEnum.ToBeDeleted);
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

            currentTenant.BuildDefinitions.Should().ContainSingle(d => d.SourceCode == forkRepoA && d.TenantCode == "TenantA" && d.State == EntityStateEnum.NotCreated);
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

            currentTenant.BuildDefinitions.Should().ContainSingle(d => d.SourceCode == forkRepoA && d.TenantCode == "TenantA" && d.State == EntityStateEnum.ToBeDeleted);
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
                BuildDefinitions = new List<BuildDefinition>(new[]
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
            currentTenant.CustomSourceRepos.Should().NotContain(f => f.State == EntityStateEnum.NotCreated);
            currentTenant.CustomSourceRepos.Should().ContainSingle(f => f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" && f.State == EntityStateEnum.ToBeDeleted);
            //build definition checks
            currentTenant.BuildDefinitions.Should().NotContain(d => d.State == EntityStateEnum.NotCreated);
            currentTenant.BuildDefinitions.Should().ContainSingle(d => d.SourceCode.SourceRepositoryName == "RepoB" && forkRepoA.TenantCode == "TenantA" && d.State == EntityStateEnum.ToBeDeleted);
        }
    }
}
