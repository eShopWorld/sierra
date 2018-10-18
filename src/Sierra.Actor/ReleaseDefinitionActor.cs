using Sierra.Common.Events;

namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Interfaces;
    using Model;
    using Common;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
    using Eshopworld.Core;

    /// <summary>
    /// this actor manages release definitions for a given tenant
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    public class ReleaseDefinitionActor : SierraActor<VstsReleaseDefinition>, IReleaseDefinitionActor
    {
        private readonly ReleaseHttpClient2 _httpClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly IBigBrother _bigBrother;

        /// <summary>
        /// Initializes a new instance of ReleaseDefinitionActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="httpClient">release client</param>
        /// <param name="vstsConfiguration">vsts configuration</param>
        public ReleaseDefinitionActor(ActorService actorService, ActorId actorId, ReleaseHttpClient2 httpClient,
            VstsConfiguration vstsConfiguration, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _httpClient = httpClient;
            _vstsConfiguration = vstsConfiguration;
            _bigBrother = bigBrother;
        }

        /// <summary>
        /// add new release definition by cloning the template
        /// </summary>
        /// <param name="model">release definition model</param>
        /// <returns>updated release definition model</returns>
        public override async Task<VstsReleaseDefinition> Add(VstsReleaseDefinition model)
        {
            var templateConfig = model.BuildDefinition.SourceCode.ProjectType == ProjectTypeEnum.WebApi
                ? _vstsConfiguration.WebApiReleaseDefinitionTemplate
                : _vstsConfiguration.WebUIReleaseDefinitionTemplate;

            //load template
            var template = await _httpClient.GetReleaseDefinitionRevision(_vstsConfiguration.VstsTargetProjectId,
                templateConfig.DefinitionId, templateConfig.RevisionId);

            //customize the template
            template.Name = model.ToString();
            //change the aliases
            var firstArtifact = template.Artifacts.First();
            firstArtifact.Alias = model.BuildDefinition.ToString();
            var sourceTrigger = (ArtifactSourceTrigger) template.Triggers.First();
            sourceTrigger.ArtifactAlias = model.BuildDefinition.ToString();
            template.Environments.ForEach(e =>
            {
                var envInput = (AgentDeploymentInput) e.DeployPhases.First().GetDeploymentInput();
                envInput.ArtifactsDownloadInput.DownloadInputs.First().Alias = model.BuildDefinition.ToString();
            });

            //re-point to build definition
            var def = firstArtifact.DefinitionReference["definition"];
            def.Id = Convert.ToString(model.BuildDefinition.VstsBuildDefinitionId, CultureInfo.InvariantCulture);
            def.Name = model.BuildDefinition.ToString();

            //set tenant specific variables
            template.Variables["TenantCode"].Value = model.TenantCode;
            template.Variables["PortNumber"].Value = "11111"; //TODO: link to port management
            var vstsDef = await _httpClient.CreateOrResetDefinition(template, _vstsConfiguration.VstsTargetProjectId);

            model.UpdateWithVstsReleaseDefinition(vstsDef.Id);

            _bigBrother.Publish(new ReleaseDefinitionCreated{DefinitionName = model.ToString()});

            return model;
        }

        /// <summary>
        /// removes existing release definition
        /// </summary>
        /// <param name="model">release definition model</param>
        /// <returns>task instance</returns>
        public override async Task Remove(VstsReleaseDefinition model)
        {
            await _httpClient.DeleteReleaseDefinitionIfFExists(_vstsConfiguration.VstsTargetProjectId,
                model.ToString());

            _bigBrother.Publish(new ReleaseDefinitionDeleted{DefinitionName = model.ToString()});
        }
    }
}