namespace Sierra.Actor
{
    using System;
    using System.Threading.Tasks;
    using Autofac.Features.Indexed;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Interfaces;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;

    /// <summary>
    /// Manages assignments of user assigned managed identities to virtual machine scale sets.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    public class ScaleSetIdentityActor : SierraActor<ScaleSetIdentity>, IScaleSetIdentityActor
    {
        public const string ActorIdPrefix = "ScaleSetIdentity:";
        private readonly IIndex<DeploymentEnvironment, IAzure> _azureFactory;
        private readonly IBigBrother _bigBrother;
        private readonly string _scaleSetId;

        public ScaleSetIdentityActor(ActorService actorService, ActorId actorId,
            IIndex<DeploymentEnvironment, IAzure> azureFactory, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _azureFactory = azureFactory;
            _bigBrother = bigBrother;
            if (actorId.Kind != ActorIdKind.String)
            {
                throw new ArgumentException($"The ScaleSetIdentity actor expects string id but got {actorId}"); 
            }

            var id = actorId.GetStringId();
            if (!id.StartsWith(ActorIdPrefix))
            {
                throw new ArgumentException(
                    $"The ScaleSetIdentity actor id must starts with '{ActorIdPrefix}' followed by a virtual machine scale set id.");
            }

            _scaleSetId = id.Substring(ActorIdPrefix.Length);
        }

        public override async Task<ScaleSetIdentity> Add(ScaleSetIdentity model)
        {
            if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
            {
                throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
            }

            var azure = _azureFactory[environment];
            var scaleSet = await azure.VirtualMachineScaleSets.GetByIdAsync(_scaleSetId);
            var identity = await azure.Identities.GetByIdAsync(model.ManagedIdentityId);

            await scaleSet.Update()
                .WithExistingUserAssignedManagedServiceIdentity(identity)
                .ApplyAsync();

            return model;
        }

        public override async Task Remove(ScaleSetIdentity model)
        {
            if (!Enum.TryParse<DeploymentEnvironment>(model.EnvironmentName, out var environment))
            {
                throw new ArgumentOutOfRangeException($"The '{model.EnvironmentName}' is not a valid environment name.");
            }

            var azure = _azureFactory[environment];
            var scaleSet = await azure.VirtualMachineScaleSets.GetByIdAsync(_scaleSetId);

            await scaleSet.Update()
                .WithoutUserAssignedManagedServiceIdentity(model.ManagedIdentityId)
                .ApplyAsync();
        }
    }
}
