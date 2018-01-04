namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    public interface ILockedPrototypeActor : IActor
    {
        Task Add(string tenant);

        Task Remove(string tenant);
    }
}
