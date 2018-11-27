namespace Sierra.Actor
{
    using System;
    using System.Threading.Tasks;
    using Autofac.Features.Indexed;
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
        private readonly IIndex<DeploymentEnvironment, IAzure> _azureFactory;
        private readonly IBigBrother _bigBrother;

        public ResourceGroupActor(ActorService actorService, ActorId actorId,
            IIndex<DeploymentEnvironment, IAzure> azureFactory, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _azureFactory = azureFactory;
            _bigBrother = bigBrother;
        }

        public override async Task<ResourceGroup> Add(ResourceGroup model)
        {
            if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
            {
                throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
            }

            var azure = _azureFactory[environment];
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
            if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
            {
                throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
            }

            var azure = _azureFactory[environment];
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
    }
}
