namespace Sierra.Api
{
    using Eshopworld.Core;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// base class for some controller base logic
    /// </summary>
    public abstract class SierraControllerBase : Controller
    {
        internal IHostingEnvironment HostingEnvironment { get; private set; }
        internal IBigBrother BigBrother { get; private set; }

        /// <summary>
        /// constructor to inject hosting environment
        /// </summary>
        /// <param name="hostingEnvironment">hosting environment descriptor</param>
        /// <param name="bigBrother">big brother instance</param>
        protected SierraControllerBase(IHostingEnvironment hostingEnvironment, IBigBrother bigBrother):base()
        {
            HostingEnvironment = hostingEnvironment;
            BigBrother = bigBrother;
        }
    }
}
