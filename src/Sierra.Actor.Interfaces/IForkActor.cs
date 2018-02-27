namespace Sierra.Actor.Interfaces
{
    using Model;
    using Microsoft.ServiceFabric.Actors;
    using System.Threading.Tasks;

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
        Task ForkRepo(Fork fork);
    }
}
