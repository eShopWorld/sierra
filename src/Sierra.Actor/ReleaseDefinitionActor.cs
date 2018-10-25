namespace Sierra.Actor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Eshopworld.DevOps;
    using Interfaces;
    using Model;
    using Common;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts.Conditions;
    using Eshopworld.Core;
    using System.Collections.Generic;
    using Microsoft.TeamFoundation.DistributedTask.WebApi;
    using Common.Events;

    /// <summary>
    /// this actor manages release definitions for a given tenant
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ReleaseDefinitionActor : SierraActor<VstsReleaseDefinition>, IReleaseDefinitionActor
    {
        private readonly ReleaseHttpClient2 _releaseHttpClient;
        private readonly VstsConfiguration _vstsConfiguration;
        private readonly IBigBrother _bigBrother;
        private readonly TaskAgentHttpClient _taskAgentHttpClient;

        private const string VstsSfUpdateTaskId = "5b931b5e-3f50-426d-9891-c4a2e0523cc1";
        private const string VstsSfUpdateTaskRegionInput = "Region";

        private const string VstsSfDeployTaskId = "c6650aa0-185b-11e6-a47d-df93e7a34c64";
        private const string VstsSfDeployTaskConnectionNameInput = "serviceConnectionName";

        /// <summary>
        /// Initializes a new instance of ReleaseDefinitionActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="releaseHttpClient">release client</param>
        /// <param name="vstsConfiguration">vsts configuration</param>
        /// <param name="bigBrother">BB instance</param>
        /// <param name="taskAgentHttpClient">task agent client instance</param>
        public ReleaseDefinitionActor(ActorService actorService, ActorId actorId, ReleaseHttpClient2 releaseHttpClient,
            VstsConfiguration vstsConfiguration, IBigBrother bigBrother, TaskAgentHttpClient taskAgentHttpClient)
            : base(actorService, actorId)
        {
            _releaseHttpClient = releaseHttpClient;
            _vstsConfiguration = vstsConfiguration;
            _bigBrother = bigBrother;
            _taskAgentHttpClient = taskAgentHttpClient;
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

            //load sf endpoints
            var connectionEndpoints =
                await _taskAgentHttpClient.GetServiceEndpointsAsync(_vstsConfiguration.VstsTargetProjectId);

            //load template
            var template = await _releaseHttpClient.GetReleaseDefinitionRevision(_vstsConfiguration.VstsTargetProjectId,
                templateConfig.DefinitionId, templateConfig.RevisionId);

            //customize the template
            template.Name = model.ToString();
            //change the aliases
            var firstArtifact = template.Artifacts.First();
            firstArtifact.Alias = model.BuildDefinition.ToString();
            //re-point to build definition
            var def = firstArtifact.DefinitionReference["definition"];
            def.Id = Convert.ToString(model.BuildDefinition.VstsBuildDefinitionId, CultureInfo.InvariantCulture);
            def.Name = model.BuildDefinition.ToString();

            var sourceTrigger = (ArtifactSourceTrigger) template.Triggers.First();
            sourceTrigger.ArtifactAlias = model.BuildDefinition.ToString();
            var clonedEnvStages =
                new List<ReleaseDefinitionEnvironment>();

            int rank = 1;

            //relink to target build definition
            foreach (var e in template.Environments)
            {
                ReleaseDefinitionEnvironment predecessor = null;
                foreach (var r in EswDevOpsSdk.RegionList)
                {
                    //CI is single region (only WE)
                    if (EswDevOpsSdk.CI_EnvironmentName.Equals(e.Name, StringComparison.OrdinalIgnoreCase) &&
                        r != Regions.WestEurope)
                        continue;

                    var regionEnv = e.DeepClone();
                    var phase = regionEnv.DeployPhases.First();
                    var envInput = (AgentDeploymentInput) phase.GetDeploymentInput();
                    envInput.ArtifactsDownloadInput.DownloadInputs.First().Alias = model.BuildDefinition.ToString();

                    regionEnv.Name = $"{e.Name} - {r}";
                    regionEnv.Rank = rank++;
                    regionEnv.Id = regionEnv.Rank;
                    //link condition to predecessor (region)                  
                    if (predecessor != null)
                        regionEnv.Conditions = new List<Condition>(new[]
                        {
                            new Condition(predecessor.Name, ConditionType.EnvironmentState, "4" /*find the documentation for this value*/)
                        });

                    //re-point to correct SF instance
                    var sfDeployStep =
                        phase.WorkflowTasks.FirstOrDefault(
                            t => t.TaskId == Guid.Parse(VstsSfDeployTaskId));

                    if (sfDeployStep == null)
                        throw new Exception(
                            $"Release template {template.Name} does not contain expected Task {VstsSfDeployTaskId} for {e.Name} environment");

                    var expectedConnectionName =
                        $"esw-{r.ToRegionCode().ToLowerInvariant()}-fabric-{e.Name.ToLowerInvariant()}";

                    var sfConnection = connectionEndpoints.FirstOrDefault(c => c.Name == expectedConnectionName);
                    if (sfConnection == null)
                        throw new Exception(
                            $"SF Endpoint {expectedConnectionName} not found in VSTS project {_vstsConfiguration.VstsTargetProjectId}");

                    sfDeployStep.Inputs[VstsSfDeployTaskConnectionNameInput] = sfConnection.Id.ToString();

                    //set region in manifest
                    var sfUpdaterStep =
                        phase.WorkflowTasks.FirstOrDefault(
                            t => t.TaskId == Guid.Parse(VstsSfUpdateTaskId));

                    if (sfUpdaterStep == null)
                        throw new Exception(
                            $"Release template {template.Name} does not contain expected Task {VstsSfUpdateTaskId} for {e.Name} environment");

                    sfUpdaterStep.Inputs[VstsSfUpdateTaskRegionInput] = r.ToRegionName();

                    clonedEnvStages.Add(regionEnv);
                    predecessor = regionEnv;
                }
            }

            template.Environments = clonedEnvStages;

            //set tenant specific variables
            template.Variables["TenantCode"].Value = model.TenantCode;
            template.Variables["PortNumber"].Value = "11111"; //TODO: link to port management

            var vstsDef = await _releaseHttpClient.CreateOrResetDefinition(template, _vstsConfiguration.VstsTargetProjectId);
            model.UpdateWithVstsReleaseDefinition(vstsDef.Id);

            _bigBrother.Publish(new ReleaseDefinitionCreated {DefinitionName = model.ToString()});

            return model;
        }

        /// <summary>
        /// removes existing release definition
        /// </summary>
        /// <param name="model">release definition model</param>
        /// <returns>task instance</returns>
        public override async Task Remove(VstsReleaseDefinition model)
        {
            await _releaseHttpClient.DeleteReleaseDefinitionIfFExists(_vstsConfiguration.VstsTargetProjectId,
                model.ToString());

            _bigBrother.Publish(new ReleaseDefinitionDeleted {DefinitionName = model.ToString()});
        }
    }
}