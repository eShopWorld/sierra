namespace Sierra.Api
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class ApiService : StatelessService
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiService"/>.
        /// </summary>
        /// <param name="context"></param>
        public ApiService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(
                        serviceContext,
                        "SierraApiEndpoint",
                        (url, listener) =>
                        {
                            return new WebHostBuilder()
                                .UseKestrel()
                                .ConfigureServices(
                                    services =>
                                    {
                                        services.AddAutofac();
                                        services.AddSingleton(serviceContext);
                                    })
                                .UseContentRoot(Directory.GetCurrentDirectory())
                                .UseStartup<Startup>()
                                .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                .UseUrls(url)
                                .Build();
                        }))
            };
        }
    }
}
