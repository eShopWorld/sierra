namespace Sierra.Api.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// probe controller
    /// </summary>
    [Route("/probe")]
    [AllowAnonymous]
    public class ProbeController : Controller
    {       
        /// <summary>
        /// probe endpoint
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return  Ok();
        }
    }
}
