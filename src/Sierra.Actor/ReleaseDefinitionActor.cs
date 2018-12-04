namespace Sierra.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using Common.Events;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.TeamFoundation.DistributedTask.WebApi;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts.Conditions;
    using Model;

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

        private const char TenantPipelineVariableSeparator = ';';

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
            VstsConfiguration vstsConfiguration, IBigBrother bigBrother, TaskAgentHttpClient taskAgentHttpClient) :
            base(actorService, actorId)
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
                ? model.RingBased ? _vstsConfiguration.WebApiRingReleaseDefinitionTemplate : _vstsConfiguration.WebApiReleaseDefinitionTemplate
                : model.RingBased ? _vstsConfiguration.WebUIRingReleaseDefinitionTemplate : _vstsConfiguration.WebUIReleaseDefinitionTemplate;


            //create (or locate)
            var clone = await ClonePipeline(model, templateConfig);

            //customize
            if (!model.RingBased)
                await CustomizeNonRingPipeline(model, clone);
            else
                CustomizeRingPipeline(model, clone);

            //persist (or update)

            var vstsDef =
                await _releaseHttpClient.CreateOrResetDefinition(clone, _vstsConfiguration.VstsTargetProjectId);

            model.UpdateWithVstsReleaseDefinition(vstsDef.Id);

            _bigBrother.Publish(new ReleaseDefinitionCreated { DefinitionName = model.ToString() });

            return model;
        }

        private void CustomizeRingPipeline(VstsReleaseDefinition model, ReleaseDefinition pipeline)
        {
            var variableName = GetTenantSizePipelineVariableName(model.TenantSize);

            if (!pipeline.Variables.ContainsKey(variableName))
                throw new Exception($"Ring template #{pipeline.Id} does not contain expected variable {variableName}");

            var tenantSubString = GetTenantPipelineVariableDefinition(model.TenantCode, 11111); //TODO: link to port management
            var varValue = pipeline.Variables[variableName].Value;
            if (!varValue.Contains(tenantSubString))
            {
                //append definition for this tenant
                if (varValue.Length != 0)
                    varValue += TenantPipelineVariableSeparator;

                varValue += tenantSubString;
            }

            pipeline.Variables[variableName].Value = varValue;

            //also re-point all stages to correct artifact
            foreach (var e in pipeline.Environments)
            {
                foreach (var p in e.DeployPhases)
                {
                    var envInput = (AgentDeploymentInput)p.GetDeploymentInput();
                    var downloadInput = envInput.ArtifactsDownloadInput.DownloadInputs.FirstOrDefault();
                    if (downloadInput == null)
                        throw new Exception($"Ring template #{pipeline.Id}, environment {e.Name} does not have expected download input");

                    downloadInput.Alias = model.BuildDefinition.ToString();
                }
            }
        }

        private static string GetTenantSizePipelineVariableName(TenantSize tenantSize)
        {
            //locate the right variable
            var variableName = $"{tenantSize.ToString()}Tenants";
            return variableName;
        }

        private string GetTenantPipelineVariableDefinition(string tenantCode, int port)
        {
            return $"{tenantCode}#{port}"; //TODO: link to port management
        }

        private async Task<ReleaseDefinition> ClonePipeline(VstsReleaseDefinition model, PipelineDefinitionConfig templateDefinition)
        {
            if (model.RingBased) //it may already exist for other tenants in the ring
            {
                var definition =
                    await _releaseHttpClient.LoadDefinitionByNameIfExists(_vstsConfiguration.VstsTargetProjectId,
                        model.ToString());

                if (definition != null)
                    return definition;
            }

            //load template
            var template = await _releaseHttpClient.GetReleaseDefinitionRevision(_vstsConfiguration.VstsTargetProjectId,
                templateDefinition.DefinitionId, templateDefinition.RevisionId);

            //customize the template
            template.Name = model.ToString();
            //change the aliases
            var firstArtifact = template.Artifacts.First();
            firstArtifact.Alias = model.BuildDefinition.ToString();
            //re-point to build definition
            var def = firstArtifact.DefinitionReference["definition"];
            def.Id = Convert.ToString(model.BuildDefinition.VstsBuildDefinitionId, CultureInfo.InvariantCulture);
            def.Name = model.BuildDefinition.ToString();
            return template;
        }

        private async Task CustomizeNonRingPipeline(VstsReleaseDefinition model, ReleaseDefinition pipeline)
        {
            //load sf endpoints
            var connectionEndpoints =
                await _taskAgentHttpClient.GetServiceEndpointsAsync(_vstsConfiguration.VstsTargetProjectId);

            //set up source trigger
            var sourceTrigger = (ArtifactSourceTrigger)pipeline.Triggers.First();
            sourceTrigger.ArtifactAlias = model.BuildDefinition.ToString();

            var clonedEnvStages = new List<ReleaseDefinitionEnvironment>();

            var rank = 1;

            //relink to target build definition
            foreach (var e in pipeline.Environments)
            {
                if (!Enum.TryParse(e.Name, true, out DeploymentEnvironment sierraEnvironment))
                    throw new Exception($"Release template #{pipeline.Id} contains unrecognized environment - {e.Name}");

                if (model.SkipEnvironments != null && model.SkipEnvironments.Contains(sierraEnvironment))
                {
                    continue;
                }

                ReleaseDefinitionEnvironment predecessor = null;

                foreach (var r in EswDevOpsSdk.GetRegionSequence(sierraEnvironment, default))
                {
                    var regionEnv = e.DeepClone();
                    var phase = regionEnv.DeployPhases.First();
                    var envInput = (AgentDeploymentInput)phase.GetDeploymentInput();
                    envInput.ArtifactsDownloadInput.DownloadInputs.First().Alias = model.BuildDefinition.ToString();

                    regionEnv.Name = $"{e.Name} - {r}";
                    regionEnv.Rank = rank++;
                    regionEnv.Id = regionEnv.Rank;
                    //link condition to predecessor (region)                  
                    if (predecessor != null)
                        regionEnv.Conditions = new List<Condition>(new[]
                        {
                            new Condition(predecessor.Name, ConditionType.EnvironmentState,
                                "4" /*find the documentation for this value*/)
                        });

                    //re-point to correct SF instance
                    var sfDeployStep =
                        phase.WorkflowTasks.FirstOrDefault(t => t.TaskId == Guid.Parse(VstsSfDeployTaskId));

                    if (sfDeployStep == null)
                        throw new Exception(
                            $"Release template #{pipeline.Id} does not contain expected Task {VstsSfDeployTaskId} for {e.Name} environment");

                    var expectedConnectionName =
                        $"esw-{r.ToRegionCode().ToLowerInvariant()}-fabric-{e.Name.ToLowerInvariant()}";

                    var sfConnection = connectionEndpoints.FirstOrDefault(c => c.Name == expectedConnectionName);
                    if (sfConnection == null)
                        throw new Exception(
                            $"SF Endpoint {expectedConnectionName} not found in VSTS project {_vstsConfiguration.VstsTargetProjectId}");

                    sfDeployStep.Inputs[VstsSfDeployTaskConnectionNameInput] = sfConnection.Id.ToString();

                    //set region in manifest
                    var sfUpdaterStep =
                        phase.WorkflowTasks.FirstOrDefault(t => t.TaskId == Guid.Parse(VstsSfUpdateTaskId));

                    if (sfUpdaterStep == null)
                        throw new Exception(
                            $"Release template {pipeline.Name} does not contain expected Task {VstsSfUpdateTaskId} for {e.Name} environment");

                    sfUpdaterStep.Inputs[VstsSfUpdateTaskRegionInput] = r.ToRegionName();

                    clonedEnvStages.Add(regionEnv);
                    predecessor = regionEnv;
                }
            }

            pipeline.Environments = clonedEnvStages;

            //set tenant specific variables
            pipeline.Variables["TenantCode"].Value = model.TenantCode;
            pipeline.Variables["PortNumber"].Value = "11111"; //TODO: link to port management
        }

        private async Task<ReleaseDefinition> RemoveTenantFromRingPipeline(string targetProject, string definitionName, string tenantCode, TenantSize tenantSize)
        {
            var definition = await _releaseHttpClient.LoadDefinitionByNameIfExists(targetProject,
                definitionName);

            if (definition == null)
                throw new Exception($"Vsts Project {targetProject} does not contain release pipeline {definitionName}");

            var varName = GetTenantSizePipelineVariableName(tenantSize);
            if (!definition.Variables.ContainsKey(varName))
                throw new Exception($"Definition #{definition.Name} does not contain expected variable {varName}");

            var tenantSubString =
                GetTenantPipelineVariableDefinition(tenantCode, 11111); //TODO: link to port management

            var varValue = definition.Variables[varName].Value;
            varValue = varValue.Replace(tenantSubString, string.Empty);
            varValue = varValue.Replace($"{TenantPipelineVariableSeparator}{TenantPipelineVariableSeparator}",
                $"{TenantPipelineVariableSeparator}");
            varValue = varValue.Trim(TenantPipelineVariableSeparator);
            definition.Variables[varName].Value = varValue;

            return definition;
        }

        private bool IsRingPipelineTenantless(ReleaseDefinition definition)
        {
            return definition.Variables.All(t => string.IsNullOrWhiteSpace(t.Value.Value));
        }

        /// <summary>
        /// removes existing release definition
        /// </summary>
        /// <param name="model">release definition model</param>
        /// <returns>task instance</returns>
        public override async Task Remove(VstsReleaseDefinition model)
        {
            if (model.RingBased)
            {
                var definition = await RemoveTenantFromRingPipeline(_vstsConfiguration.VstsTargetProjectId, model.ToString(), model.TenantCode, model.TenantSize);

                //is the pipeline now empty? if so, actually delete
                if (!IsRingPipelineTenantless(definition))
                {
                    await _releaseHttpClient.CreateOrResetDefinition(definition, _vstsConfiguration.VstsTargetProjectId);
                    _bigBrother.Publish(new ReleaseDefinitionUpdated { DefinitionName = model.ToString() });
                    return;
                }
            }

            await _releaseHttpClient.DeleteReleaseDefinitionIfFExists(_vstsConfiguration.VstsTargetProjectId,
                model.ToString());

            _bigBrother.Publish(new ReleaseDefinitionDeleted { DefinitionName = model.ToString() });
        }
    }
}