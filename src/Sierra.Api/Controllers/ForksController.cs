namespace Sierra.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using Model;
    using Actor.Interfaces;
    using Microsoft.AspNetCore.Hosting;
    using Eshopworld.Core;

    /// <summary>
    /// manages forks in the system
    /// </summary>
    [Route("/v1/forks")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    //todo: temporary testing controller
    public class ForksController : SierraControllerBase
    {
        /// <inheritdoc/>
        public ForksController(IHostingEnvironment hostingEnvironment, IBigBrother bigBrother) : base(hostingEnvironment, bigBrother)
        {
        }

        /// <summary>
        /// creates a fork
        /// </summary>
        /// <param name="fork">fork definition</param>
        [HttpPost]
        public async Task Post([FromBody]Fork fork)
        {
            var actor = this.GetActorRef<IForkActor>("ForkActorService");
            await actor.ForkRepo(fork);
        }
    }
}
