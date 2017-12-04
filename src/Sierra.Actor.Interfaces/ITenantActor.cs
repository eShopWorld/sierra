namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    /// <summary>
    /// Defines a common contract for the TenantActor.
    /// </summary>
    public interface ITenantActor : IActor
    {
        /// <summary>
        /// Adds a tenant to the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task Add(Tenant tenant);

        /// <summary>
        /// Removes a tenant from the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task Remove(Tenant tenant);

        /// <summary>
        /// Changes a tenant in the platform.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task Edit(Tenant tenant);
    }
}
