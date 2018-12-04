namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    public interface IManagedIdentityActor : IActor
    {
        Task<ManagedIdentity> Add(ManagedIdentity assignment);

        Task Remove(ManagedIdentity assignment);
    }
}
