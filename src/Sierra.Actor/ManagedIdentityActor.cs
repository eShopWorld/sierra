namespace Sierra.Actor
{
    using System;
    using System.Collections.Generic;
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
            var scaleSetName = model.VirtualMachineScaleSetName;
            var scaleSetResourceGroupName = model.VirtualMachineScaleSetResourceGroupName;
            try
            {
                var azure = BuildAzureClient(model.EnvironmentName);
                subscriptionId = azure.SubscriptionId;

                stage = "resourceGroupValidation";
                var resourceGroup = await azure.ResourceGroups.GetByNameAsync(model.ResourceGroupName);

                stage = "scaleSetValidation";
                var scaleSets = await FindScaleSets(azure, scaleSetName, scaleSetResourceGroupName);

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

                foreach (var scaleSet in scaleSets)
                {
                    if (!IsIdentityAssigned(scaleSet, identity))
                    {
                        stage = "identityAssignment";
                        scaleSetName = scaleSet.Name;
                        scaleSetResourceGroupName = scaleSet.ResourceGroupName;
                        await scaleSet.Update()
                            .WithExistingUserAssignedManagedServiceIdentity(identity)
                            .ApplyAsync();
                    }
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
                    VirtualMachineScaleSetName = scaleSetName,
                    VirtualMachineScaleSetResourceGroupName = scaleSetResourceGroupName,
                };
                _bigBrother.Publish(errorEvent);
                throw;
            }
        }

        public override async Task Remove(ManagedIdentityAssignment model)
        {
            var stage = "initialization";
            string subscriptionId = null;
            var scaleSetName = model.VirtualMachineScaleSetName;
            var scaleSetResourceGroupName = model.VirtualMachineScaleSetResourceGroupName;
            try
            {
                var azure = BuildAzureClient(model.EnvironmentName);
                subscriptionId = azure.SubscriptionId;

                stage = "resourceGroupValidation";
                if (!await azure.ResourceGroups.ContainAsync(model.ResourceGroupName))
                {
                    return;
                }

                stage = "identityFinding";
                var identities = await azure.Identities.ListByResourceGroupAsync(model.ResourceGroupName);
                var identity = identities.FirstOrDefault(x => x.Name == model.IdentityName);
                if (identity == null)
                {
                    return;
                }

                stage = "scaleSetFinding";
                var scaleSets = await FindScaleSets(azure, scaleSetName, scaleSetResourceGroupName);

                foreach (var scaleSet in scaleSets)
                {
                    stage = "identityUnassignment";
                    scaleSetName = scaleSet.Name;
                    scaleSetResourceGroupName = scaleSet.ResourceGroupName;
                    await scaleSet.Update()
                        .WithoutUserAssignedManagedServiceIdentity(identity.Id)
                        .ApplyAsync();
                }

                stage = "identityDeletion";
                await azure.Identities.DeleteByIdAsync(identity.Id);
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
                    VirtualMachineScaleSetName = scaleSetName,
                    VirtualMachineScaleSetResourceGroupName = scaleSetResourceGroupName,
                };
                _bigBrother.Publish(errorEvent);
                throw;
            }
        }


        private static async Task<List<IVirtualMachineScaleSet>> FindScaleSets(IAzure azure, string scaleSetName, string scaleSetResourceGroupName)
        {
            List<IVirtualMachineScaleSet> scaleSets;
            if (scaleSetName != null)
            {
                var scaleSetsList = await azure.VirtualMachineScaleSets.ListByResourceGroupAsync(
                    scaleSetResourceGroupName);
                var scaleSet = scaleSetsList.FirstOrDefault(
                    x => scaleSetName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                if (scaleSet == null)
                {
                    throw new Exception("The specified virtual machine scale set has not been found.");
                }

                scaleSets = new List<IVirtualMachineScaleSet> { scaleSet };
            }
            else if (scaleSetResourceGroupName != null)
            {
                scaleSets = (await azure.VirtualMachineScaleSets.ListByResourceGroupAsync(
                    scaleSetResourceGroupName)).ToList();
            }
            else
            {
                scaleSets = (await azure.VirtualMachineScaleSets.ListAsync()).ToList();
            }

            return scaleSets;
        }

        private IAzure BuildAzureClient(string environmentName)
        {
            var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(environmentName);
            return _authenticated.WithSubscription(subscriptionId);
        }

        private static bool IsIdentityAssigned(IVirtualMachineScaleSet scaleSet, IIdentity identity)
        {
            var isAssigned = scaleSet.ManagedServiceIdentityType == ResourceIdentityType.SystemAssignedUserAssigned
                              || scaleSet.ManagedServiceIdentityType == ResourceIdentityType.UserAssigned;

            if (!isAssigned) return false;

            return scaleSet.UserAssignedManagedServiceIdentityIds != null
                   && scaleSet.UserAssignedManagedServiceIdentityIds.Contains(identity.Id);
        }
    }
}
