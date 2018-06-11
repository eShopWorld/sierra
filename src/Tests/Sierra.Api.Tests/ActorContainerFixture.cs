namespace Sierra.Api.Tests
{
    using System;
    using Autofac;
    using Common.DependencyInjection;
    using Xunit;

    public class ActorContainerFixture : IDisposable
    {
        internal readonly IContainer Container;

        public ActorContainerFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreModule { Vault = @"https://esw-tooling-ci.vault.azure.net/" });
            builder.RegisterModule(new VstsModule());

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
