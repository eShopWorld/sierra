using System.Threading.Tasks;

namespace Sierra.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using Actor.Interfaces;

    /// <summary>
    /// manages forks in the system
    /// </summary>
    [Route("/v1/forks")]
    //todo: temporary testing controller
    public class ForksController : Controller
    {       
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
