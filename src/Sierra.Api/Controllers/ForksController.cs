namespace Sierra.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
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
#if (OAUTH_OFF_MODE)
    [AllowAnonymous]
#else
    [Authorize(Policy = "AssertScope")]
#endif
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
            await actor.Add(fork);
        }

        /// <summary>
        /// delete a fork
        /// </summary>
        /// <param name="fork">fork to delete</param>
        /// <returns>task instance</returns>       
        [HttpDelete("{fork}")]
        public async Task Delete(string fork)
        {
            var actor = GetActorRef<IForkActor>("ForkActorService");
            await actor.Remove(fork);
        }
    }
}
