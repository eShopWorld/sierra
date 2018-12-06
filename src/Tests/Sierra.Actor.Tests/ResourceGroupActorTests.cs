using System;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Sierra.Model;
using Xunit;

[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class ResourceGroupActorTests
{
    private const string TestResourceGroupName = "TestResourceGroup";
    private ActorTestsFixture Fixture { get; }

    public ResourceGroupActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    private ResourceGroup TestResourceGroupRequest()
    {
        return new ResourceGroup
        {
            Name = TestResourceGroupName,
            Environment = Fixture.Environment,
        };
    }

    [Theory, IsLayer2]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddTest(bool resourceGroupExists)
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            var azure = scope.ResolveKeyed<IAzure>(EswDevOpsSdk.GetEnvironment());
            await PrepareResourceGroup(resourceGroupExists, azure);
            try
            {
                var rg = TestResourceGroupRequest();
                var actorResponse = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ResourceGroup", "Add", rg);
                actorResponse.Should().NotBeNull();
                actorResponse.ResourceId.Should().NotBeNullOrEmpty();
                actorResponse.State.Should().Be(EntityStateEnum.Created);

                await Task.Delay(TimeSpan.FromSeconds(5));
                var isResourceGroupCreated = await azure.ResourceGroups.ContainAsync(TestResourceGroupName);
                isResourceGroupCreated.Should()
                    .BeTrue($"the resource {TestResourceGroupName} group should be created in the {azure.SubscriptionId} subscription, but the actor reported that it created {actorResponse.ResourceId}.");
                var createdResourceGroup = await azure.ResourceGroups.GetByNameAsync(TestResourceGroupName);
                createdResourceGroup.Should().NotBeNull();
                createdResourceGroup.Region.Should().Be(Fixture.TestRegion);
                actorResponse.ResourceId.Should().Be(createdResourceGroup.Id);
            }
            finally
            {
                await DeleteResourceGroup(azure, TestResourceGroupName);
            }
        }
    }

    [Theory, IsLayer2]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemoveTests(bool resourceGroupExists)
    {
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var cl = scope.Resolve<HttpClient>();
            var azure = scope.ResolveKeyed<IAzure>(EswDevOpsSdk.GetEnvironment());
            await PrepareResourceGroup(resourceGroupExists, azure);

            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ResourceGroup", "Remove", TestResourceGroupRequest());

                await Task.Delay(TimeSpan.FromSeconds(5));
                var exists = await azure.ResourceGroups.ContainAsync(TestResourceGroupName);
                exists.Should().BeFalse();
            }
            finally
            {
                await DeleteResourceGroup(azure, TestResourceGroupName);
            }
        }
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

    private static async Task EnsureResourceGroupExists(IAzure azure, string resourceGroupName, Region region)
    {
        if (!await azure.ResourceGroups.ContainAsync(resourceGroupName))
        {
            await azure.ResourceGroups.Define(resourceGroupName)
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
}
