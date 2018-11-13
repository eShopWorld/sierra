namespace Sierra.Actor
{
    using System;
    using System.Threading.Tasks;
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
        private readonly Func<Azure.IAuthenticated> _authenticated;
        private readonly IBigBrother _bigBrother;
        private readonly string _scaleSetId;

        public ScaleSetIdentityActor(ActorService actorService, ActorId actorId,
            Func<Azure.IAuthenticated> authenticated, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _authenticated = authenticated;
            _bigBrother = bigBrother;
            if (actorId.Kind != ActorIdKind.String)
            {
                throw new Exception($"The ScaleSetIdentity actor expects string id but got {actorId}");
            }

            var id = actorId.GetStringId();
            if (!id.StartsWith(ActorIdPrefix))
            {
                throw new Exception(
                    $"The ScaleSetIdentity actor id must starts with '{ActorIdPrefix}' followed by a virtual machine scale set id.");
            }

            _scaleSetId = id.Substring(ActorIdPrefix.Length);
        }

        public override async Task<ScaleSetIdentity> Add(ScaleSetIdentity model)
        {
            var azure = BuildAzureClient(model.EnvironmentName);

            var scaleSet = await azure.VirtualMachineScaleSets.GetByIdAsync(_scaleSetId);
            var identity = await azure.Identities.GetByIdAsync(model.ManagedIdentityId);

            await scaleSet.Update()
                .WithExistingUserAssignedManagedServiceIdentity(identity)
                .ApplyAsync();

            return model;
        }

        public override async Task Remove(ScaleSetIdentity model)
        {
            var azure = BuildAzureClient(model.EnvironmentName);

            var scaleSet = await azure.VirtualMachineScaleSets.GetByIdAsync(_scaleSetId);

            await scaleSet.Update()
                .WithoutUserAssignedManagedServiceIdentity(model.ManagedIdentityId)
                .ApplyAsync();
        }

        private IAzure BuildAzureClient(string environmentName)
        {
            var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(environmentName);
            return _authenticated().WithSubscription(subscriptionId);
        }
    }
}
