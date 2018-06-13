namespace Sierra.Api
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;

    /// <summary>
    /// extension class for controllers
    /// </summary>
    internal static class ControllerExtensions
    {
        /// <summary>
        /// get actor proxy via SF API
        /// </summary>
        /// <typeparam name="T">desired interface</typeparam>
        /// <param name="ctrl">controller instance for the extension method</param>
        /// <param name="serviceName">name of the service</param>
        /// <returns>proxy instance</returns>
        internal static T GetActorRef<T>(this SierraControllerBase ctrl, string serviceName) where T:IActor
        {
            //todo: add support for explicit actor id            
            return ActorProxy.Create<T>(ActorId.CreateRandom(), new Uri($"fabric:/Sierra.Fabric{DeriveEnvironmentSuffix(ctrl.HostingEnvironment)}/{serviceName}")); 
        }

        private static object DeriveEnvironmentSuffix(IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment.IsDevelopment())
                return string.Empty;

            return $"-{hostingEnvironment.EnvironmentName}";
        }
    }
}
