namespace Sierra.Actor
{
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Interfaces;
    using System.Threading.Tasks;
    using Microsoft.TeamFoundation.Build.WebApi;
    using BuildDefinition = Model.BuildDefinition;
    using Sierra.Common;

    [StatePersistence(StatePersistence.Persisted)]
    public class BuildDefinitionActor : SierraActor<BuildDefinition>, IBuildDefinitionActor
    {
        private readonly BuildHttpClient _buildHttpClient;
        private readonly VstsConfiguration _vstsConfiguration;

        /// <summary>
        /// Initializes a new instance of BuildDefinitionActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public BuildDefinitionActor(ActorService actorService, ActorId actorId, BuildHttpClient buildHttpClient, VstsConfiguration vstsConfig) 
            : base(actorService, actorId)
        {
            _buildHttpClient = buildHttpClient;
            _vstsConfiguration = vstsConfig;
        }

        /// <inheridoc/>
        public override async Task<BuildDefinition> Add(BuildDefinition model)
        {           
            return model;
        }

        /// <inheridoc/>
        public override async Task Remove(BuildDefinition model)
        {
        }
    }
}
