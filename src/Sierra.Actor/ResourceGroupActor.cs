namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Common.Events;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Interfaces;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;

    /// <summary>
    /// Creates and deletes resource groups.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ResourceGroupActor : SierraActor<ResourceGroup>, IResourceGroupActor
    {
        private readonly Azure.IAuthenticated _authenticated;
        private readonly IBigBrother _bigBrother;

        public ResourceGroupActor(ActorService actorService, ActorId actorId,
            Azure.IAuthenticated authenticated, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _authenticated = authenticated;
            _bigBrother = bigBrother;
        }

        public override async Task<ResourceGroup> Add(ResourceGroup model)
        {
            var azure = BuildAzureClient(model.EnvironmentName);
            IResourceGroup resourceGroup;
            if (await azure.ResourceGroups.ContainAsync(model.Name))
            {
                resourceGroup = await azure.ResourceGroups.GetByNameAsync(model.Name);
            }
            else
            {
                resourceGroup = await azure.ResourceGroups
                        .Define(model.Name)
                        .WithRegion(Region.EuropeNorth)
                        .CreateAsync();

                _bigBrother.Publish(new ResourceGroupCreated
                {
                    EnvironmentName = model.EnvironmentName,
                    RegionName = resourceGroup.RegionName,
                    ResourceId = resourceGroup.Id,
                    ResourceGroupName = resourceGroup.Name,
                });
            }

            model.State = EntityStateEnum.Created;
            model.ResourceId = resourceGroup.Id;
            return model;
        }

        public override async Task Remove(ResourceGroup model)
        {
            var azure = BuildAzureClient(model.EnvironmentName);
            if (await azure.ResourceGroups.ContainAsync(model.Name))
            {
                await azure.ResourceGroups
                    .DeleteByNameAsync(model.Name);

                _bigBrother.Publish(new ResourceGroupDeleted
                {
                    EnvironmentName = model.EnvironmentName,
                    ResourceGroupName = model.Name
                });
            }
        }

        private IAzure BuildAzureClient(string environmentName)
        {
            var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(environmentName);
            return _authenticated.WithSubscription(subscriptionId);
        }
    }
}
