namespace Sierra.Api
{
    using Eshopworld.Core;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using System;
    using System.Fabric;

    /// <summary>
    /// base class for some controller base logic
    /// </summary>
    public abstract class SierraControllerBase : Controller
    {
        internal IHostingEnvironment HostingEnvironment { get; private set; }
        internal IBigBrother BigBrother { get; private set; }
        internal StatelessServiceContext StatelessServiceContext { get; private set; }

        /// <summary>
        /// constructor to inject hosting environment
        /// </summary>
        /// <param name="hostingEnvironment">hosting environment descriptor</param>
        /// <param name="bigBrother">big brother instance</param>
        /// <param name="sfContext">service fabric context</param>
        protected SierraControllerBase(IHostingEnvironment hostingEnvironment, IBigBrother bigBrother, StatelessServiceContext sfContext) :base()
        {
            HostingEnvironment = hostingEnvironment;
            BigBrother = bigBrother;
            StatelessServiceContext = sfContext;
        }

        /// <summary>
        /// get actor proxy via SF API
        /// </summary>
        /// <typeparam name="T">desired interface</typeparam>
        /// <param name="serviceName">name of the service</param>
        /// <returns>proxy instance</returns>
        internal  T GetActorRef<T>(string serviceName) where T : IActor
        {
            var actorUri = new Uri($"{StatelessServiceContext.CodePackageActivationContext.ApplicationName}/{serviceName}");                 

            return ActorProxy.Create<T>(ActorId.CreateRandom(), actorUri);
        }
    }
}
