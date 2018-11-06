using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using Polly;
using Sierra.Model;
using Xunit;

[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class ManagedIdentityActorTests
{
    private const string TestResourceGroupName = "TestResourceGroup";
    private const string ScaleSetName = "SierraTestSS";
    private const string ScaleSetResourceGroupName = "SierraTest";
    private const string IdentityName = "SierraTestSSIdentity";
    private ActorTestsFixture Fixture { get; }

    public ManagedIdentityActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    private IAzure InitAzure(ILifetimeScope scope)
    {
        var azureAuth = scope.Resolve<Azure.IAuthenticated>();
        return azureAuth.WithSubscription(Fixture.DeploymentSubscriptionId);
    }

    private ManagedIdentityAssignment CreateMangedIdentityAssignment()
    {
        return new ManagedIdentityAssignment
        {
            ResourceGroupName = TestResourceGroupName,
            IdentityName = IdentityName,
            VirtualMachineScaleSetName = ScaleSetName,
            VirtualMachineScaleSetResourceGroupName = ScaleSetResourceGroupName,
            EnvironmentName = Fixture.EnvironmentName,
        };
    }

    [Theory, IsLayer2]
    [InlineData(OperationPhase.IdentityNotCreated)]
    [InlineData(OperationPhase.IdentityCreatedAndNotAssigned)]
    [InlineData(OperationPhase.IdentityAssigned)]
    public async Task AddTest(OperationPhase operationPhase)
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var azure = InitAzure(scope);
            var resourceGroup = await EnsureResourceGroupExists(azure, TestResourceGroupName, Region.EuropeNorth);
            var scaleSet = await GetScaleSet(azure);
            await Prepare(azure, operationPhase, resourceGroup, scaleSet, IdentityName);

            var msi = CreateMangedIdentityAssignment();
            var actorResponse = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ManagedIdentity", "Add", msi);
            actorResponse.Should().NotBeNull();
            actorResponse.IdentityId.Should().NotBeNullOrEmpty();
            actorResponse.State.Should().Be(EntityStateEnum.Created);

            var policy = Policy
                .Handle<CloudException>()
                .OrResult<IIdentity>(x => x == null)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            var capturedIdentity =
                await policy.ExecuteAndCaptureAsync(() => azure.Identities.GetByIdAsync(actorResponse.IdentityId));
            capturedIdentity.Result.Should().NotBeNull(
                    $"the identity {actorResponse.IdentityId} should be created in {azure.SubscriptionId} subscription");
            capturedIdentity.Result.Name.Should().Be(msi.IdentityName);
            capturedIdentity.Result.ResourceGroupName.Should().Be(msi.ResourceGroupName);

            var modifiedScaleSet = await GetScaleSet(azure);
            var isAssigned = IsIdentityAssigned(azure, modifiedScaleSet, capturedIdentity.Result);
            isAssigned.Should()
                .BeTrue($"the identity {capturedIdentity.Result.Id} should be assigned to the scale set {modifiedScaleSet.Id}");
        }
    }

    [Theory(Skip = ""), IsLayer2]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemoveTests(bool resourceGroupExists)
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var azure = InitAzure(scope);
            await PrepareResourceGroup(resourceGroupExists, azure);

            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ResourceGroup", "Remove", CreateMangedIdentityAssignment());

                // repeat un
                var policy = Policy
                    .HandleResult<bool>(x => x)
                    .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    );

                var exists = await policy.ExecuteAsync(() => azure.ResourceGroups.ContainAsync(TestResourceGroupName));
                exists.Should().BeFalse($"The resource group {TestResourceGroupName} should be deleted");
            }
            finally
            {
                await DeleteResourceGroup(azure, TestResourceGroupName);
            }
        }
    }

    private async Task<IVirtualMachineScaleSet> GetScaleSet(IAzure azure)
    {
        var sets = await azure.VirtualMachineScaleSets.ListByResourceGroupAsync(ScaleSetResourceGroupName);
        var scaleSet =
            sets.FirstOrDefault(x => x.Name == ScaleSetName && x.ResourceGroupName == ScaleSetResourceGroupName);
        scaleSet.Should()
            .NotBeNull(
                $"the VM scale set {ScaleSetName} (in the resource group {ScaleSetResourceGroupName}) is required as a prerequisite of the test.");
        return scaleSet;
    }

    private async Task PrepareResourceGroup(bool resourceGroupExists, IAzure azure)
    {
        if (resourceGroupExists)
        {
            await EnsureResourceGroupExists(azure, TestResourceGroupName, Fixture.TestRegion);

        }
        else
        {
            await DeleteResourceGroup(azure, TestResourceGroupName);
        }
    }

    private static async Task<IResourceGroup> EnsureResourceGroupExists(IAzure azure, string resourceGroupName, Region region)
    {
        if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
        {
            return await azure.ResourceGroups.GetByNameAsync(resourceGroupName);
        }
        else
        {
            return await azure.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(region)
                    .CreateAsync();
        }
    }

    private static async Task DeleteResourceGroup(IAzure azure, string resourceGroupName)
    {
        if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
        {
            await azure.ResourceGroups.DeleteByNameAsync(resourceGroupName);
        }
    }

    public enum OperationPhase
    {
        IdentityNotCreated,
        IdentityCreatedAndNotAssigned,
        IdentityAssigned,
    }

    private async Task Prepare(IAzure azure, OperationPhase phase, IResourceGroup resourceGroup,
        IVirtualMachineScaleSet scaleSet, string identityName)
    {
        var identity = await FindIdentity(azure, resourceGroup, identityName);
        var isAssigned = identity != null && IsIdentityAssigned(azure, scaleSet, identity);
        switch (phase)
        {
            case OperationPhase.IdentityNotCreated:
                if (isAssigned)
                {
                    await scaleSet.Update()
                        .WithoutUserAssignedManagedServiceIdentity(identity.Id)
                        .ApplyAsync();
                }
                if (identity != null)
                {
                    await azure.Identities.DeleteByIdAsync(identity.Id);
                }
                break;
            case OperationPhase.IdentityCreatedAndNotAssigned:
                if (identity == null)
                {
                    identity = await CreateIdentity(azure, resourceGroup, identityName);
                }

                if (isAssigned)
                {
                    await scaleSet.Update()
                        .WithoutUserAssignedManagedServiceIdentity(identity.Id)
                        .ApplyAsync();
                }
                break;
            case OperationPhase.IdentityAssigned:
                if (identity == null)
                {
                    identity = await CreateIdentity(azure, resourceGroup, identityName);
                }

                if (!isAssigned)
                {
                    await scaleSet.Update()
                        .WithExistingUserAssignedManagedServiceIdentity(identity)
                        .ApplyAsync();
                }
                break;
        }

    }

    private static async Task<IIdentity> CreateIdentity(IAzure azure, IResourceGroup resourceGroup, string identityName)
    {
        return await azure.Identities
             .Define(identityName)
             .WithRegion(resourceGroup.RegionName)
             .WithExistingResourceGroup(resourceGroup)
             .CreateAsync();
    }

    private static async Task<IIdentity> FindIdentity(IAzure azure, IResourceGroup resourceGroup, string identityName)
    {
        var identities = await azure.Identities.ListByResourceGroupAsync(resourceGroup.Name);
        return identities.FirstOrDefault(x => x.Name == identityName);
    }

    private static bool IsIdentityAssigned(IAzure azure, IVirtualMachineScaleSet scaleSet, IIdentity identity)
    {
        bool isAssigned = scaleSet.ManagedServiceIdentityType == ResourceIdentityType.SystemAssignedUserAssigned
                          || scaleSet.ManagedServiceIdentityType == ResourceIdentityType.UserAssigned;

        if (!isAssigned) return false;

        return scaleSet.UserAssignedManagedServiceIdentityIds != null
               && scaleSet.UserAssignedManagedServiceIdentityIds.Contains(identity.Id);
    }
}
