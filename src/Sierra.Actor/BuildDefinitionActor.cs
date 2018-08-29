namespace Sierra.Actor
{
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Interfaces;
    using Sierra.Model;
    using System.Threading.Tasks;

    [StatePersistence(StatePersistence.Persisted)]
    public class BuildDefinitionActor : SierraActor<BuildDefinition>, IBuildDefinitionActor
    {
        /// <summary>
        /// Initializes a new instance of BuildDefinitionActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public BuildDefinitionActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        public override Task<BuildDefinition> Add(BuildDefinition model)
        {
            throw new System.NotImplementedException();
        }

        public override Task Remove(BuildDefinition model)
        {
            throw new System.NotImplementedException();
        }
    }
}
