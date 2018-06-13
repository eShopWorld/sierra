namespace Sierra.Api
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// base class for some controller base logic
    /// </summary>
    public abstract class SierraControllerBase : Controller
    {
        internal IHostingEnvironment HostingEnvironment { get; private set; }

        /// <summary>
        /// constructor to inject hosting environment
        /// </summary>
        /// <param name="hostingEnvironment">hosting environment descriptor</param>
        internal SierraControllerBase(IHostingEnvironment hostingEnvironment):base()
        {
            HostingEnvironment = hostingEnvironment;
        }
    }
}
