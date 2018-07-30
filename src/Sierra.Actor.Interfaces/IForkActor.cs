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
        Task Remove(Fork fork);
    }
}
