namespace Sierra.Actor
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Common;
    using Common.Events;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.Azure.Management.Fluent;
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
        private readonly IBigBrother _bigBrother;
        private readonly IAzure _azure;

        public ResourceGroupActor(ActorService actorService, ActorId actorId,
            Azure.IAuthenticated authenticated, EnvironmentConfiguration environmentConfiguration,
            IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _bigBrother = bigBrother;

            var actorStringId = actorId.GetStringId();
            var match = Regex.Match(actorStringId, "-([A-Z]+)$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                throw new ArgumentException($"The format of the actor Id {actorId} is invalid (the environment name is missing).");
            }

            var environmentName = match.Groups[1].Value;
            if (!environmentConfiguration.EnvironmentSubscriptionMap.TryGetValue(environmentName,
                out var subscriptionId))
            {
                throw new ArgumentException($"The actor Id {actorId} does not contain a recognized environment name.");
            }
            _azure = authenticated.WithSubscription(subscriptionId);
        }

        public override async Task<ResourceGroup> Add(ResourceGroup model)
        {
            if (await _azure.ResourceGroups.ContainAsync(model.Name))
            {
                return model;
            }

            await _azure.ResourceGroups
                .Define(model.Name)
                .WithRegion(Region.EuropeNorth)
                .CreateAsync();

            // TODO: will the event contain the actorId or any other part of the context?
            _bigBrother.Publish(new ResourceGroupCreated
            {
                ResourceGroupName = model.Name,
            });

            return model;
        }

        public override async Task Remove(ResourceGroup model)
        {
            if (await _azure.ResourceGroups.ContainAsync(model.Name))
            {
                await _azure.ResourceGroups
                    .DeleteByNameAsync(model.Name);

                _bigBrother.Publish(new ResourceGroupDeleted { ResourceGroupName = model.Name });
            }
        }
    }
}
