namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    /// <summary>
    /// fork actor interface
    /// </summary>
    public interface IRepositoryActor : IActor
    {
        /// <summary>
        /// fork repository
        /// </summary>
        /// <param name="sourceCodeRepository">payload to describe requested fork</param>
        /// <returns>task instance wrapper with resulting <see cref="SourceCodeRepository"/></returns>
        Task<SourceCodeRepository> Add(SourceCodeRepository sourceCodeRepository);

        /// <summary>
        /// remove an existing repo (if exists)
        /// </summary>
        /// <param name="sourceCodeRepository">repo to remove</param>
        /// <returns>task instance</returns>
        Task Remove(SourceCodeRepository sourceCodeRepository);
    }
}
