namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    /// <summary>
    /// Defines a common contract for the base SierraActor.
    /// </summary>
    public interface ISierraActor : IActor
    {
        Task Add(Tenant tenant);

        Task Remove(Tenant tenant);
    }
}
