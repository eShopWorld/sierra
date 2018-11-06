namespace Sierra.Actor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Events;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Interfaces;
    using Microsoft.Azure.Management.Compute.Fluent;
    using Microsoft.Azure.Management.Compute.Fluent.Models;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.Msi.Fluent;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Sierra.Model;

    [StatePersistence(StatePersistence.Volatile)]
    public class ManagedIdentityActor : SierraActor<ManagedIdentityAssignment>, IManagedIdentityActor
    {
        private readonly Azure.IAuthenticated _authenticated;
        private readonly IBigBrother _bigBrother;

        public ManagedIdentityActor(ActorService actorService, ActorId actorId,
            Azure.IAuthenticated authenticated, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _authenticated = authenticated;
            _bigBrother = bigBrother;
        }

        public override async Task<ManagedIdentityAssignment> Add(ManagedIdentityAssignment model)
        {
            var stage = "initialization";
            string subscriptionId = null;
            try
            {
                var azure = BuildAzureClient(model.EnvironmentName);
                subscriptionId = azure.SubscriptionId;

                stage = "resourceGroupValidation";
                var resourceGroup = await azure.ResourceGroups.GetByNameAsync(model.ResourceGroupName);

                stage = "scaleSetValidation";
                var scaleSetsList = await azure.VirtualMachineScaleSets.ListByResourceGroupAsync(model.VirtualMachineScaleSetResourceGroupName);
                var scaleSet = scaleSetsList.FirstOrDefault(
                    x => model.VirtualMachineScaleSetName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                if (scaleSet == null)
                {
                    throw new Exception("The specified virtual machine scale set has not been found.");
                }

                stage = "identityFinding";
                var identities = await azure.Identities.ListByResourceGroupAsync(model.ResourceGroupName);
                var identity = identities.FirstOrDefault(x => x.Name == model.IdentityName);

                if (identity == null)
                {
                    stage = "identityCreation";
                    identity = await azure.Identities
                        .Define(model.IdentityName)
                        .WithRegion(resourceGroup.RegionName)
                        .WithExistingResourceGroup(resourceGroup)
                        .CreateAsync();
                }

                model.IdentityId = identity.Id;

                if (!IsIdentityAssigned(azure, scaleSet, identity))
                {
                    stage = "identityAssignment";
                    await scaleSet.Update()
                        .WithExistingUserAssignedManagedServiceIdentity(identity)
                        .ApplyAsync();
                }

                model.State = EntityStateEnum.Created;
                return model;
            }
            catch (Exception e)
            {
                var errorEvent = new ManagedIdentityActorError(e)
                {
                    Stage = stage,
                    SubscriptionId = subscriptionId,
                    EnvironmentName = model.EnvironmentName,
                    IdentityName = model.IdentityName,
                    ResourceGroupName = model.ResourceGroupName,
                    VirtualMachineScaleSetName = model.VirtualMachineScaleSetName,
                    VirtualMachineScaleSetResourceGroupName = model.VirtualMachineScaleSetResourceGroupName,
                };
                _bigBrother.Publish(errorEvent);
                throw;
            }
        }

        public override Task Remove(ManagedIdentityAssignment model)
        {
            var azure = BuildAzureClient(model.EnvironmentName);
            throw new System.NotImplementedException();
        }
        private IAzure BuildAzureClient(string environmentName)
        {
            var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(environmentName);
            return _authenticated.WithSubscription(subscriptionId);
        }

        private static bool IsIdentityAssigned(IAzure azure, IVirtualMachineScaleSet scaleSet, IIdentity identity)
        {
            var isAssigned = scaleSet.ManagedServiceIdentityType == ResourceIdentityType.SystemAssignedUserAssigned
                              || scaleSet.ManagedServiceIdentityType == ResourceIdentityType.UserAssigned;

            if (!isAssigned) return false;

            return scaleSet.UserAssignedManagedServiceIdentityIds != null
                   && scaleSet.UserAssignedManagedServiceIdentityIds.Contains(identity.Id);
        }
    }
}
