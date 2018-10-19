namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    /// <summary>
    /// Defines a common contract for the ResourceGroupActor.
    /// </summary>
    public interface IResourceGroupActor : IActor
    {
        /// <summary>
        /// Adds a resource group to the platform.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task<ResourceGroup> Add(ResourceGroup resourceGroup);

        /// <summary>
        /// Removes a resource group from the platform.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task Remove(ResourceGroup resourceGroup);
    }
}
