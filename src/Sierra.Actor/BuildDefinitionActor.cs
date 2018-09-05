// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace Sierra.Actor
{
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Interfaces;
    using System.Threading.Tasks;
    using Microsoft.TeamFoundation.Build.WebApi;
    using BuildDefinition = Model.BuildDefinition;
    using Common;
    using Eshopworld.Core;
    using Common.Events;

    [StatePersistence(StatePersistence.Persisted)]
    public class BuildDefinitionActor : SierraActor<BuildDefinition>, IBuildDefinitionActor
    {
        private readonly BuildHttpClient _buildHttpClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly IBigBrother _bigBrother;

        /// <summary>
        /// Initializes a new instance of BuildDefinitionActor
        /// </summary>
        /// <param name="actorService">instance of <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">instance of <see cref="ActorId"/> for this actor instance.</param>
        /// <param name="buildHttpClient">http build client</param>
        /// <param name="vstsConfig">vsts configuration</param>
        /// <param name="bb">big brother instance</param>
        public BuildDefinitionActor(ActorService actorService, ActorId actorId, BuildHttpClient buildHttpClient, VstsConfiguration vstsConfig, IBigBrother bb) 
            : base(actorService, actorId)
        {
            _buildHttpClient = buildHttpClient;
            _vstsConfiguration = vstsConfig;
            _bigBrother = bb;
        }

        /// <inheritdoc cref="SierraActor{T}" />
        public override async Task<BuildDefinition> Add(BuildDefinition model)
        {
            //load template
            var template = await _buildHttpClient.GetDefinitionAsync(_vstsConfiguration.VstsTargetProjectId,
                _vstsConfiguration.WebApiBuildDefinitionTemplate.DefinitionId,
                _vstsConfiguration.WebApiBuildDefinitionTemplate.RevisionId);
            //customize the template
            template.Name = model.ToString();
            template.Repository.Id = model.SourceCode.ForkVstsId.ToString();
            //push to vsts
            var vstsDefinition =
                await _buildHttpClient.CreateOrUpdateDefinition(template, _vstsConfiguration.VstsTargetProjectId);
            //update model
            model.UpdateWithVstsDefinition(vstsDefinition.Id);

            _bigBrother.Publish(new BuildDefinitionCreated {DefinitionName = vstsDefinition.Name});

            return model;
        }

        /// <inheritdoc cref="SierraActor{T}" />
        public override async Task Remove(BuildDefinition model)
        {
            var name = model.ToString();

            await _buildHttpClient.DeleteDefinitionIfExists(_vstsConfiguration.VstsTargetProjectId, name);

            _bigBrother.Publish(new BuildDefinitionDeleted {DefinitionName = name});
        }
    }
}
