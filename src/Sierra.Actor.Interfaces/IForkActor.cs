namespace Sierra.Actor.Interfaces
{
    using Model;
    using Microsoft.ServiceFabric.Actors;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// fork actor interface
    /// </summary>
    public interface IForkActor : IActor
    {
        /// <summary>
        /// fork repository
        /// </summary>
        /// <param name="fork">payload to describe requested fork</param>
        /// <returns>task instance</returns>
        Task Add(Fork fork);

        /// <summary>
        /// remove an existing repo (if exists)
        /// </summary>
        /// <param name="fork">repo to remove</param>
        /// <returns>task instance</returns>
        Task Remove(string fork);

        /// <summary>
        /// retrieve a list of existing forks that have been created for a given tenant
        /// 
        /// this follows tenant fork naming conventions
        /// </summary>
        /// <param name="tenantName">tenant name</param>
        /// <returns>list of tenanted/forked repos</returns>
        Task<IEnumerable<string>> QueryTenantRepos(string tenantName);
    }
}
