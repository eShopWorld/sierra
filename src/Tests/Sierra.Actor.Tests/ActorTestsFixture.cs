using System;
using Autofac;
using Eshopworld.DevOps;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
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

    public string EnvironmentName { get; private set; }

    public string TestMiddlewareUri { get; private set; }

    public Region TestRegion { get; private set; }

    public string DeploymentSubscriptionId { get; private set; }

    public ActorTestsFixture()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule(true));
        builder.RegisterModule(new VstsModule());
        builder.RegisterModule(new AzureManagementFluentModule());
        builder.Register(c => new SierraDbContext
            { ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] });

        builder.Register(c =>
        {
            var testConfig = new TestConfig();

            var config = EswDevOpsSdk.BuildConfiguration(true);
            config.GetSection("TestConfig").Bind(testConfig);
            
            TestMiddlewareUri = $"{testConfig.ApiUrl}test";
            TestRegion = string.IsNullOrEmpty(testConfig.RegionName)
                ? Region.EuropeNorth
                : Region.Create(testConfig.RegionName);

            EnvironmentName = testConfig.SubscriptionName;
            DeploymentSubscriptionId = "0b50e185-2e2a-4e1c-bf2f-ead0b80e0b79";  // sierra integration

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