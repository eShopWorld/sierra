
namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

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
