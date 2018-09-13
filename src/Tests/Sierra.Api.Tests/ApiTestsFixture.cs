namespace Sierra.Api.Tests
{
    using System;
    using Autofac;
    using Common.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Model;
    using Xunit;
    using Eshopworld.DevOps;

    public class ApiTestsFixture : IDisposable
    {
        internal readonly IContainer Container;        

        public ApiTestsFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreModule(true));
            builder.RegisterModule(new VstsModule());
            builder.Register(c => new SierraDbContext { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] });
            builder.Register(c =>
            {
                var testConfig = new TestConfig();
                var config = EswDevOpsSdk.BuildConfiguration(true);
                config.GetSection("TestConfig").Bind(testConfig);
                return testConfig;
            });

            Container = builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }

    [CollectionDefinition(nameof(ActorContainerCollection))]
    public class ActorContainerCollection : ICollectionFixture<ApiTestsFixture> { }
}
