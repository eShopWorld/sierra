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
    using Sierra.Model;
    using Microsoft.EntityFrameworkCore;

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
                builder.RegisterModule(new CoreModule { Vault = @"https://esw-tooling-ci.vault.azure.net/" });
                builder.RegisterModule(new VstsModule());

                builder.RegisterServiceFabricSupport();               

                builder.RegisterActor<TenantActor>();
                builder.RegisterActor<LockerActor>();
                builder.RegisterActor<ForkActor>();

                builder.Register(c => 
                {
                    var ctx = new SierraDbContext { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] };
                    ctx.Database.Migrate(); //can also be used as fail-safe option if not running migrations within CD pipeline
                    return ctx;
                });

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
