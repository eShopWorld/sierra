using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
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
    private ActorTestsFixture Fixture { get; }
    private static readonly ResourceGroup TestResourceGroup = new ResourceGroup
    {
        Name = "TestResourceGroup",
    };


    public ResourceGroupActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    private IAzure InitAzure(ILifetimeScope scope)
    {
        var azureAuth = scope.Resolve<Azure.IAuthenticated>();
        return azureAuth.WithSubscription(Fixture.SubscriptionId);
    }

    [Theory, IsLayer2]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddTest(bool resourceGroupExists)
    {
        var cl = new HttpClient();
        using (var scope = Fixture.Container.BeginLifetimeScope())
        { 
            var azure = InitAzure(scope);
            await PrepareResourceGroup(resourceGroupExists, azure);
            try
            {
                var resourceGroupCreated = await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ResourceGroup", "Add", TestResourceGroup, actorId: "test-CI");
                resourceGroupCreated.Should().NotBeNull();

                var createdResourceGroup = await azure.ResourceGroups.GetByNameAsync(TestResourceGroup.Name);
                createdResourceGroup.Should().NotBeNull();
                createdResourceGroup.Region.Should().Be(Fixture.TestRegion);
            }
            finally
            {
                await DeleteResourceGroup(azure, TestResourceGroup.Name);
            }
        }
    }

    [Theory, IsLayer2]
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
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "ResourceGroup", "Remove", TestResourceGroup, actorId: "test-CI");

                var exists = await azure.ResourceGroups.ContainAsync(TestResourceGroup.Name);
                exists.Should().BeFalse();
            }
            finally
            {
                await DeleteResourceGroup(azure, TestResourceGroup.Name);
            }
        }
    }

    private async Task PrepareResourceGroup(bool resourceGroupExists, IAzure azure)
    {
        if (resourceGroupExists)
        {
            await EnsureResourceGroupExists(azure, TestResourceGroup.Name, Fixture.TestRegion);

        }
        else
        {
            await DeleteResourceGroup(azure, TestResourceGroup.Name);
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