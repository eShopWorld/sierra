﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Sierra.Common.DependencyInjection;
using Sierra.Common.Tests;
using Sierra.Model;
using Xunit;

/// <summary>
/// this fixture serves as a L3 E2E orchestrator
/// 
/// fixture ensure tenant is created and destroyed at the beginning and end of the collection of tests
/// 
/// individual tests within the collection then check for their respective concerns
/// 
/// later consider adding generics to this to allow for different tenant + sublevels configuration for different scenarios
/// </summary>
// ReSharper disable once CheckNamespace
public class TenantL3TestFixture : IDisposable
{
    public readonly IContainer Container;

    public TestConfig TestConfig;
    //preset tenant code
    public string TenantCode = "CITNT";
    //preset fork source repository name
    public string ForkSourceRepo = "ForkIntTestSourceRepo";

    /// <summary>
    /// upon creation, load the tenant DB record into memory for tests
    /// </summary>
    public Tenant TenantUnderTest { get; }

    private string TenantsControllerUrl => $"{TestConfig.ApiUrl}tenants";
    private static readonly TimeSpan TenantApiTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// constructor logic
    ///
    /// it invokes the tenant api to create the tenant so that other tests dependent on this fixture do not have to concern with the set up
    /// </summary>
    public TenantL3TestFixture()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule(true));
        builder.RegisterModule(new VstsModule());
        builder.Register(c => new SierraDbContext
            {ConnectionString = c.Resolve<IConfigurationRoot>()["SierraDbConnectionString"] });
        builder.Register(c =>
        {
            TestConfig = new TestConfig();
            var config = EswDevOpsSdk.BuildConfiguration(true);
            config.GetSection("TestConfig").Bind(TestConfig);
            return TestConfig;
        });

        Container = builder.Build();
        TestConfig = Container.Resolve<TestConfig>();
        EnsureTenantCreated().Wait(TenantApiTimeout);
        //load tenant into memory
        using (var scope = Container.BeginLifetimeScope())
        {
            var dbContext = scope.Resolve<SierraDbContext>();
            TenantUnderTest = dbContext.LoadCompleteTenantAsync(TenantCode).Result;
        }
    }

    private async Task EnsureTenantCreated()
    {
        //define complete tenant (with all supported/testable artifacts)
        var newTenant = new Tenant
        {
            Code = TenantCode,
            Name = $"Tenant{TenantCode}",
            SourceRepos =
                new List<SourceCodeRepository>(new[] { new SourceCodeRepository { SourceRepositoryName = ForkSourceRepo, Fork = true } })
        };

        var client = new HttpClient();
        //obtain access token
        var stsAccessToken = await ObtainSTSAccessToken();
        client.SetBearerToken(stsAccessToken);

        //add tenant
        var resp = await client.PostAsJsonAsync(TenantsControllerUrl, newTenant);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// fixture dispose
    /// 1. ensure tenant deleted
    /// 2. dispose of the container
    /// </summary>
    public void Dispose()
    {
        EnsureTenantDeleted().Wait(TenantApiTimeout);
        Container.Dispose();
    }


    private async Task EnsureTenantDeleted()
    {
        var client = new HttpClient();
        //obtain access token
        var stsAccessToken = await ObtainSTSAccessToken();
        client.SetBearerToken(stsAccessToken);

        var resp = await client.DeleteAsync($"{TenantsControllerUrl}/{TenantCode}");
        resp.EnsureSuccessStatusCode();
    }

    // ReSharper disable once InconsistentNaming
    private async Task<string> ObtainSTSAccessToken()
    {
        var httpClient = new HttpClient();
        var discovery = await httpClient.GetDiscoveryDocumentAsync(TestConfig.STSAuthority);
        var options = new TokenClientOptions
        {
            Address = discovery.TokenEndpoint,
            ClientId = TestConfig.STSClientId,
            ClientSecret = TestConfig.STSClientSecret,
        };
        var client = new TokenClient(httpClient, options);
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(TestConfig.STSScope);
        return tokenResponse.AccessToken;
    }
}

[CollectionDefinition(nameof(TenantL3Collection))]
public class TenantL3Collection : ICollectionFixture<TenantL3TestFixture>
{
}