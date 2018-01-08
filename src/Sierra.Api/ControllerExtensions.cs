using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace Sierra.Api
{
    /// <summary>
    /// extension class for controllers
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// get actor proxy via SF API
        /// </summary>
        /// <typeparam name="T">desired interface</typeparam>
        /// <param name="ctrl">controller instance for the extension method</param>
        /// <param name="serviceName">name of the service</param>
        /// <returns>proxy instance</returns>
        public static T GetActorRef<T>(this Controller ctrl, string serviceName) where T:IActor
        {
            //todo: we can probably derive service name from the interface under default naming conventions
            //todo: add support for explicit actor id
            return ActorProxy.Create<T>(ActorId.CreateRandom(), new Uri($"fabric:/Sierra.Fabric/{serviceName}")); 
        }
    }
}
