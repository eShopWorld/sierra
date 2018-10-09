using System;
using Autofac;
using Eshopworld.DevOps;
using Microsoft.Extensions.Configuration;
using Sierra.Common.DependencyInjection;
using Sierra.Common.Tests;
using Sierra.Model;
using Xunit;

/// <summary>
/// (collection) fixture for actor direct tests
///
/// this sets up key artifacts including KV so that any details allowing end system tests (e.g. VSTS) are available
/// </summary>
// ReSharper disable once CheckNamespace
public class ActorTestsFixture : IDisposable
{
    public readonly IContainer Container;

    public string TestMiddlewareUri { get; set; }

    public ActorTestsFixture()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule(true));
        builder.RegisterModule(new VstsModule());
        builder.Register(c => new SierraDbContext
            { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] });

        builder.Register(c =>
        {
            var testConfig = new TestConfig();
            var config = EswDevOpsSdk.BuildConfiguration(true);
            config.GetSection("TestConfig").Bind(testConfig);

            TestMiddlewareUri = $"{testConfig.ApiUrl}test";

            return testConfig;
        });       

        Container = builder.Build();
        //trigger set up
        Container.Resolve<TestConfig>();
    }

    public void Dispose()
    {
        Container.Dispose();
    }
}

[CollectionDefinition(nameof(ActorTestsCollection))]
public class ActorTestsCollection : ICollectionFixture<ActorTestsFixture>
{
}