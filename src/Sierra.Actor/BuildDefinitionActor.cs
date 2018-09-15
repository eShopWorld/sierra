namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Common;
    using Common.Events;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.TeamFoundation.Build.WebApi;
    using Model;

    [StatePersistence(StatePersistence.Volatile)]
    public class BuildDefinitionActor : SierraActor<VstsBuildDefinition>, IBuildDefinitionActor
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
        public override async Task<VstsBuildDefinition> Add(VstsBuildDefinition model)
        {
            var defId = model.SourceCode.ProjectType == ProjectTypeEnum.WebApi
                ? _vstsConfiguration.WebApiBuildDefinitionTemplate.DefinitionId
                : _vstsConfiguration.WebUIBuildDefinitionTemplate.DefinitionId;

            var revId = model.SourceCode.ProjectType == ProjectTypeEnum.WebApi
                ? _vstsConfiguration.WebApiBuildDefinitionTemplate.RevisionId
                : _vstsConfiguration.WebUIBuildDefinitionTemplate.RevisionId;

            //load template
            var template = await _buildHttpClient.GetDefinitionAsync(_vstsConfiguration.VstsTargetProjectId,
                defId, revId);

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
        public override async Task Remove(VstsBuildDefinition model)
        {
            var name = model.ToString();

            await _buildHttpClient.DeleteDefinitionIfExists(_vstsConfiguration.VstsTargetProjectId, name);

            _bigBrother.Publish(new BuildDefinitionDeleted {DefinitionName = name});
        }
    }
}
