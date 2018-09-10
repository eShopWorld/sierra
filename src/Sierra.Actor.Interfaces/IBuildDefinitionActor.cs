
namespace Sierra.Actor.Interfaces
{
    using Microsoft.ServiceFabric.Actors;
    using Model;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IBuildDefinitionActor : IActor
    {
        Task<VstsBuildDefinition> Add(VstsBuildDefinition def);

        Task Remove(VstsBuildDefinition def);
    }
}
