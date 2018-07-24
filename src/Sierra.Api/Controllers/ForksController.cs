namespace Sierra.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using Model;
    using Actor.Interfaces;
    using Microsoft.AspNetCore.Hosting;
    using Eshopworld.Core;
    using System.Fabric;

    /// <summary>
    /// manages forks in the system
    /// </summary>
    [Route("/v1/forks")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    //todo: temporary testing controller
    public class ForksController : SierraControllerBase
    {
        /// <inheritdoc/>
        public ForksController(IHostingEnvironment hostingEnvironment, IBigBrother bigBrother, StatelessServiceContext sfContext) : base(hostingEnvironment, bigBrother, sfContext)
        {
        }

        /// <summary>
        /// creates a fork
        /// </summary>
        /// <param name="fork">fork definition</param>
        [HttpPost]        
        public async Task Post([FromBody]Fork fork)
        {
            var actor = GetActorRef<IForkActor>("ForkActorService");
            await actor.AddFork(fork);
        }

        /// <summary>
        /// delete a fork
        /// </summary>
        /// <param name="forkName">name of the fork to delete</param>
        /// <returns>task instance</returns>       
        [HttpDelete("{forkName}")]
        public async Task Delete(string forkName)
        {
            var actor = GetActorRef<IForkActor>("ForkActorService");
            await actor.RemoveFork(forkName);
        }
    }
}
