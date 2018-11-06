namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    public interface IManagedIdentityActor: IActor
    {
        Task<ManagedIdentityAssignment> Add(ManagedIdentityAssignment assignment);

        Task Remove(ManagedIdentityAssignment assignment);
    }
}
