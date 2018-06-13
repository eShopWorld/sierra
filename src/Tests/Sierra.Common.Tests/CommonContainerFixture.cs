namespace Sierra.Common.Tests
{
    using System;
    using Autofac;
    using Common.DependencyInjection;
    using Xunit;

    public class CommonContainerFixture : IDisposable
    {
        internal readonly IContainer Container;

        public CommonContainerFixture()
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

    [CollectionDefinition(nameof(CommonContainerCollection))]
    public class CommonContainerCollection : ICollectionFixture<CommonContainerFixture> { }
}
