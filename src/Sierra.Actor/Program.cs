namespace Sierra.Actor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Integration.ServiceFabric;
    using Common.DependencyInjection;
    using Eshopworld.Telemetry;
    using Microsoft.Extensions.Configuration;

    internal static class Program
    {
        /// <summary>
        /// The entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule(new VstsModule {Vault = @"https://esw-tooling-ci.vault.azure.net/"});

                builder.RegisterServiceFabricSupport();

                builder.Register(c =>
                {
                    var insKey = c.Resolve<IConfigurationRoot>()["BBInstrumentationKey"];
                    return new BigBrother(insKey, insKey);
                })
                .SingleInstance();

                builder.RegisterActor<TenantActor>();
                builder.RegisterActor<LockerActor>();
                builder.RegisterActor<ForkActor>();

                using (builder.Build())
                {
                    await Task.Delay(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }
    }
}
