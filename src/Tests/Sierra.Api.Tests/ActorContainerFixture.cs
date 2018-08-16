namespace Sierra.Api.Tests
{
    using System;
    using Autofac;
    using Common.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Sierra.Model;
    using Xunit;

    public class ActorContainerFixture : IDisposable
    {
        internal readonly IContainer Container;

        public ActorContainerFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreModule { Vault = @"https://esw-tooling-ci.vault.azure.net/" });
            builder.RegisterModule(new VstsModule());
            builder.Register(c => new SierraDbContext { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] });

            Container = builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }

    [CollectionDefinition(nameof(ActorContainerCollection))]
    public class ActorContainerCollection : ICollectionFixture<ActorContainerFixture> { }
}
