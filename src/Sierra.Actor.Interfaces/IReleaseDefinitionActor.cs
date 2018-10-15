namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    public interface IReleaseDefinitionActor : IActor
    {
        Task<VstsReleaseDefinition> Add(VstsReleaseDefinition def);

        Task Remove(VstsReleaseDefinition def);
    }
}