namespace Sierra.Actor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac.Features.Indexed;
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
    public class ManagedIdentityActor : SierraActor<ManagedIdentity>, IManagedIdentityActor
    {
        private readonly IIndex<DeploymentEnvironment, IAzure> _azureFactory;
        private readonly IBigBrother _bigBrother;

        public ManagedIdentityActor(ActorService actorService, ActorId actorId,
            IIndex<DeploymentEnvironment, IAzure> azureFactory, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _azureFactory = azureFactory;
            _bigBrother = bigBrother;
        }

        public override async Task<ManagedIdentity> Add(ManagedIdentity model)
        {
            // TODO: remove this custom error logging when a more generic solution is available
            var stage = "initialization";
            string subscriptionId = null;
            try
            {
                if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
                {
                    throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
                }

                var azure = _azureFactory[environment];
                subscriptionId = azure.SubscriptionId;

                stage = "resourceGroupValidation";
                var resourceGroup = await azure.ResourceGroups.GetByNameAsync(model.ResourceGroupName);

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

                stage = "scaleSetValidation";
                var scaleSets = await azure.VirtualMachineScaleSets.ListAsync();

                stage = "scaleSetIdentityAssignment";
                Task AssignIdentityToScaleSet(IVirtualMachineScaleSet scaleSet) =>
                    GetActor<IScaleSetIdentityActor>(ScaleSetIdentityActor.ActorIdPrefix + scaleSet.Id)
                        .Add(new ScaleSetIdentity
                        {
                            EnvironmentName = model.EnvironmentName,
                            ManagedIdentityId = identity.Id,
                        });
                
                var scaleSetAssignmentTasks = scaleSets.Select(AssignIdentityToScaleSet);

                await Task.WhenAll(scaleSetAssignmentTasks);

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
                };
                _bigBrother.Publish(errorEvent);
                throw;
            }
        }

        public override async Task Remove(ManagedIdentity model)
        {
            var stage = "initialization";
            string subscriptionId = null;
            try
            {
                if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
                {
                    throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
                }

                var azure = _azureFactory[environment];
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
                var scaleSets = await azure.VirtualMachineScaleSets.ListAsync();

                stage = "scaleSetIdentityUnassignment";
                Task UnassignIdentityFromScaleSet(IVirtualMachineScaleSet scaleSet) =>
                    GetActor<IScaleSetIdentityActor>(ScaleSetIdentityActor.ActorIdPrefix + scaleSet.Id)
                        .Remove(new ScaleSetIdentity
                        {
                            EnvironmentName = model.EnvironmentName,
                            ManagedIdentityId = identity.Id,
                        });

                var scaleSetAssignmentTasks = scaleSets.Select(UnassignIdentityFromScaleSet);

                await Task.WhenAll(scaleSetAssignmentTasks);

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
                };
                _bigBrother.Publish(errorEvent);
                throw;
            }
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
