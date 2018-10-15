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
    using Model;
    using Microsoft.EntityFrameworkCore;
    using Eshopworld.Core;

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
                builder.RegisterModule(new CoreModule());
                builder.RegisterModule(new VstsModule());

                builder.RegisterServiceFabricSupport();               

                builder.RegisterActor<TenantActor>();
                builder.RegisterActor<LockerActor>();
                builder.RegisterActor<ForkActor>();
                builder.RegisterActor<TestActor>();
                builder.RegisterActor<BuildDefinitionActor>();
                builder.RegisterActor<ReleaseDefinitionActor>();

                builder.Register(c => new SierraDbContext { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"]});              

                using (var container = builder.Build())
                {
                    try
                    {
                        var dbCtx = container.Resolve<SierraDbContext>();
                        dbCtx.Database.Migrate();
                    }
                    catch (Exception e)
                    {
                        container.Resolve<IBigBrother>().Publish(e.ToExceptionEvent());
                    }                   
                   
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
