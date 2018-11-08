using Eshopworld.Tests.Core;
using FluentAssertions;
using Sierra.Model;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// ReSharper disable once CheckNamespace
public class TenantTests
{
    [Fact, IsUnit]
    public void Update_AddAdditionalRepo()
    {
        var fork = new SourceCodeRepository
        {
            SourceRepositoryName = "AlreadyThere", State = EntityStateEnum.Created, TenantCode = "TenantA"
        };
        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[] {fork}),
            BuildDefinitions = new List<VstsBuildDefinition>(new[]
            {
                new VstsBuildDefinition
                {
                    SourceCode = fork, TenantCode = "TenantA", State = EntityStateEnum.Created
                }
            })
        };

        var tenantRequest = new Tenant
        {
            SourceRepos = new List<SourceCodeRepository>(new[]
            {
                new SourceCodeRepository {SourceRepositoryName = "A"},
                new SourceCodeRepository {SourceRepositoryName = "AlreadyThere"}
            })
        };

        currentTenant.Update(tenantRequest);
        currentTenant.SourceRepos.Should().HaveCount(2);
        //forks checks
        currentTenant.SourceRepos.Should().ContainSingle(f => f.State == EntityStateEnum.NotCreated);
        currentTenant.SourceRepos.Should()
            .ContainSingle(f =>
                f.SourceRepositoryName == "A" && f.TenantCode == "TenantA" && f.State == EntityStateEnum.NotCreated);
        currentTenant.SourceRepos.Should()
            .Contain(f =>
                f.SourceRepositoryName == "AlreadyThere" && f.State == EntityStateEnum.Created &&
                f.TenantCode == "TenantA");
        //build definition checks
        currentTenant.BuildDefinitions.Should().ContainSingle(bd => bd.State == EntityStateEnum.NotCreated);
        currentTenant.BuildDefinitions.Should()
            .ContainSingle(d =>
                d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" &&
                d.SourceCode.SourceRepositoryName == "A");
        currentTenant.BuildDefinitions.Should().NotContain(bd => bd.State == EntityStateEnum.ToBeDeleted);
        //release definition checks
        currentTenant.ReleaseDefinitions.Should().ContainSingle(bd => bd.State == EntityStateEnum.NotCreated);
        currentTenant.ReleaseDefinitions.Should()
            .ContainSingle(d =>
                d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" &&
                d.BuildDefinition.SourceCode.SourceRepositoryName == "A");
        currentTenant.ReleaseDefinitions.Should().NotContain(bd => bd.State == EntityStateEnum.ToBeDeleted);
    }

    [Fact, IsUnit]
    public void Update_AddRepo()
    {
        var currentTenant = new Tenant
        {
            Code = "TenantA", Name = "oldName", SourceRepos = new List<SourceCodeRepository>()
        };

        var tenantRequest = new Tenant
        {
            SourceRepos =
                new List<SourceCodeRepository>(new[] {new SourceCodeRepository {SourceRepositoryName = "RepoB"}})
        };

        currentTenant.Update(tenantRequest);

        //fork checks
        currentTenant.SourceRepos.Should()
            .OnlyContain(f =>
                f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" &&
                f.State == EntityStateEnum.NotCreated);
        //build definition checks       
        currentTenant.BuildDefinitions.Should()
            .OnlyContain(d =>
                d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" &&
                d.SourceCode.SourceRepositoryName == "RepoB");
        //release definition checks
        currentTenant.ReleaseDefinitions.Should()
            .OnlyContain(d =>
                d.State == EntityStateEnum.NotCreated && d.TenantCode == "TenantA" &&
                d.BuildDefinition.SourceCode.SourceRepositoryName == "RepoB");
    }

    [Fact, IsUnit]
    public void Update_CheckNotCreatedForkRetry()
    {
        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[]
            {
                new SourceCodeRepository
                {
                    SourceRepositoryName = "RepoA",
                    State = EntityStateEnum.NotCreated,
                    TenantCode = "TenantA"
                }
            })
        };

        var tenantRequest = new Tenant
        {
            SourceRepos = new List<SourceCodeRepository>(new[]
            {
                new SourceCodeRepository {SourceRepositoryName = "RepoA"},
                new SourceCodeRepository {SourceRepositoryName = "RepoB"}
            })
        };

        currentTenant.Update(tenantRequest);

        currentTenant.SourceRepos.Should()
            .OnlyContain(f =>
                (f.SourceRepositoryName == "RepoA" || f.SourceRepositoryName == "RepoB") && f.TenantCode == "TenantA");
    }

    [Fact, IsUnit]
    public void Update_CheckToBeDeletedForkRetry()
    {
        var forkRepoA = new SourceCodeRepository
        {
            SourceRepositoryName = "RepoA", State = EntityStateEnum.ToBeDeleted, TenantCode = "TenantA"
        };
        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[] {forkRepoA}),
            BuildDefinitions = new List<VstsBuildDefinition>(new[]
            {
                new VstsBuildDefinition
                {
                    SourceCode = forkRepoA, State = EntityStateEnum.Created, TenantCode = "TenantA"
                }
            })
        };

        var tenantRequest = new Tenant
        {
            SourceRepos = new List<SourceCodeRepository>(new[]
            {
                new SourceCodeRepository {SourceRepositoryName = "RepoA"},
                new SourceCodeRepository {SourceRepositoryName = "RepoB"}
            })
        };

        currentTenant.Update(tenantRequest);

        currentTenant.SourceRepos.Should()
            .ContainSingle(f =>
                f.SourceRepositoryName == "RepoA" && f.TenantCode == "TenantA" &&
                f.State == EntityStateEnum.ToBeDeleted);
    }

    [Fact, IsUnit]
    public void Update_CheckNotCreatedBuildDefinitionRetry()
    {
        var forkRepoA = new SourceCodeRepository
        {
            SourceRepositoryName = "RepoA", State = EntityStateEnum.Created, TenantCode = "TenantA"
        };
        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[] {forkRepoA}),
            BuildDefinitions = new List<VstsBuildDefinition>(new[]
            {
                new VstsBuildDefinition
                {
                    SourceCode = forkRepoA, State = EntityStateEnum.NotCreated, TenantCode = "TenantA"
                }
            })
        };

        var tenantRequest = new Tenant
        {
            SourceRepos = new List<SourceCodeRepository>(new[]
            {
                new SourceCodeRepository {SourceRepositoryName = "RepoA"}
            })
        };
        currentTenant.Update(tenantRequest);

        currentTenant.BuildDefinitions.Should()
            .ContainSingle(d =>
                d.SourceCode.ToString() == forkRepoA.ToString() && d.TenantCode == "TenantA" &&
                d.State == EntityStateEnum.NotCreated);
    }

    [Fact, IsUnit]
    public void Update_RemoveRepo()
    {
        var forkRepoA = new SourceCodeRepository
        {
            State = EntityStateEnum.Created, SourceRepositoryName = "RepoA", TenantCode = "TenantA"
        };
        var forkRepoB = new SourceCodeRepository
        {
            State = EntityStateEnum.Created, SourceRepositoryName = "RepoB", TenantCode = "TenantA"
        };
        var relDefA = new VstsReleaseDefinition {TenantCode = "TenantA", State = EntityStateEnum.Created};
        var relDefB = new VstsReleaseDefinition {TenantCode = "TenantB", State = EntityStateEnum.Created};
        var buildDefA = new VstsBuildDefinition
        {
            SourceCode = forkRepoA,
            TenantCode = "TenantA",
            ReleaseDefinition = relDefA,
            State = EntityStateEnum.Created
        };
        var buildDefB = new VstsBuildDefinition
        {
            SourceCode = forkRepoB,
            TenantCode = "TenantA",
            ReleaseDefinition = relDefB,
            State = EntityStateEnum.Created
        };

        relDefA.BuildDefinition = buildDefA;
        relDefB.BuildDefinition = buildDefB;

        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[] {forkRepoA, forkRepoB}),
            BuildDefinitions = new List<VstsBuildDefinition>(new[] {buildDefA, buildDefB}),
            ReleaseDefinitions = new List<VstsReleaseDefinition>(new[] {relDefA, relDefB})
        };

        var tenantRequest = new Tenant
        {
            SourceRepos =
                new List<SourceCodeRepository>(new[] {new SourceCodeRepository {SourceRepositoryName = "RepoA"}})
        };

        currentTenant.Update(tenantRequest);

        //forks checks
        currentTenant.SourceRepos.Should().NotContain(f => f.State == EntityStateEnum.NotCreated);
        currentTenant.SourceRepos.Should()
            .ContainSingle(f =>
                f.SourceRepositoryName == "RepoB" && f.TenantCode == "TenantA" &&
                f.State == EntityStateEnum.ToBeDeleted);
        //build definition checks
        currentTenant.BuildDefinitions.Should().NotContain(d => d.State == EntityStateEnum.NotCreated);
        currentTenant.BuildDefinitions.Should()
            .ContainSingle(d =>
                d.SourceCode.SourceRepositoryName == "RepoB" && forkRepoA.TenantCode == "TenantA" &&
                d.State == EntityStateEnum.ToBeDeleted);
        //release definition checks
        currentTenant.ReleaseDefinitions.Should().NotContain(d => d.State == EntityStateEnum.NotCreated);
        currentTenant.ReleaseDefinitions.Should()
            .ContainSingle(d =>
                d.BuildDefinition.SourceCode.SourceRepositoryName == "RepoB" && forkRepoA.TenantCode == "TenantA" &&
                d.State == EntityStateEnum.ToBeDeleted);
    }

    [Fact, IsUnit]
    public void SwitchForkToStandardComponent()
    {
        var forkRepoA = new SourceCodeRepository
        {
            State = EntityStateEnum.Created,
            SourceRepositoryName = "RepoA",
            TenantCode = "TenantA",
            Fork = true
        };
        var relDefA = new VstsReleaseDefinition { TenantCode = "TenantA", State = EntityStateEnum.Created };
        var buildDefA = new VstsBuildDefinition
        {
            SourceCode = forkRepoA,
            TenantCode = "TenantA",
            ReleaseDefinition = relDefA,
            State = EntityStateEnum.Created
        };

        relDefA.BuildDefinition = buildDefA;

        var currentTenant = new Tenant
        {
            Code = "TenantA",
            Name = "oldName",
            SourceRepos = new List<SourceCodeRepository>(new[] {forkRepoA}),
            BuildDefinitions = new List<VstsBuildDefinition>(new[] { buildDefA }),
            ReleaseDefinitions = new List<VstsReleaseDefinition>(new[] { relDefA })
        };

        var tenantRequest = new Tenant
        {
            SourceRepos =
                new List<SourceCodeRepository>(new[]
                    {new SourceCodeRepository {SourceRepositoryName = "RepoA", Fork = false}})
        };

        currentTenant.Update(tenantRequest);
        //check fork repo in to be deleted state and standard component in to be created
        currentTenant.SourceRepos.Should().HaveCount(2);
        currentTenant.SourceRepos.Should().ContainSingle(r =>
            r.Fork && r.SourceRepositoryName == "RepoA" && r.State == EntityStateEnum.ToBeDeleted);
        currentTenant.SourceRepos.Should().ContainSingle(r =>
            !r.Fork && r.SourceRepositoryName == "RepoA" && r.State == EntityStateEnum.NotCreated);
        //check the same for build definition
        var newRepo = currentTenant.SourceRepos.First(r => !r.Fork);

        currentTenant.BuildDefinitions.Should().HaveCount(2);
        currentTenant.BuildDefinitions.Should().ContainSingle(b =>
            b.SourceCode.Equals(forkRepoA) && b.State == EntityStateEnum.ToBeDeleted);
        currentTenant.BuildDefinitions.Should().ContainSingle(b =>
            b.SourceCode.Equals(newRepo) && b.State == EntityStateEnum.NotCreated);
        
        //check the same for release definition
        var newBuildDef = currentTenant.BuildDefinitions.First(r => r.SourceCode.Equals(newRepo));

        currentTenant.ReleaseDefinitions.Should().HaveCount(2);
        currentTenant.ReleaseDefinitions.Should()
            .ContainSingle(r => r.BuildDefinition.Equals(buildDefA) && r.State == EntityStateEnum.ToBeDeleted);
        currentTenant.ReleaseDefinitions.Should()
            .ContainSingle(r => r.BuildDefinition.Equals(newBuildDef) && r.State == EntityStateEnum.NotCreated);
    }
}