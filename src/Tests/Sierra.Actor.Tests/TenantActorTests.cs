using System;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Sierra.Model;
using Xunit;

[Collection(nameof(ActorTestsCollection))]
// ReSharper disable once CheckNamespace
public class TenantActorTests
{
    private ActorTestsFixture Fixture { get; }

    private const string L2TenantCode = "L2TNT";

    public TenantActorTests(ActorTestsFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact, IsLayer2]
    public async Task AddTest()
    {
        var cl = new HttpClient { Timeout = TimeSpan.FromSeconds(200) };

        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var dbContext = scope.Resolve<SierraDbContext>();

            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Tenant", "Add",
                    new Tenant {Code = L2TenantCode, Name = "Tenant Name"});
                dbContext.Tenants.Should().ContainSingle(t => t.Code == L2TenantCode && t.Name== "Tenant Name");
            }
            finally
            {
                 await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Tenant", "Remove",
                    new Tenant {Code = L2TenantCode});
            }
        }
    }

    [Fact, IsLayer2]
    public async Task RemoveTest()
    {
        var cl = new HttpClient{Timeout = TimeSpan.FromSeconds(200)};
        using (var scope = Fixture.Container.BeginLifetimeScope())
        {
            var dbContext = scope.Resolve<SierraDbContext>();

            try
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Tenant", "Add",
                    new Tenant { Code = L2TenantCode, Name = "Tenant Name" });
            }
            finally
            {
                await cl.PostJsonToActor(Fixture.TestMiddlewareUri, "Tenant", "Remove",
                    new Tenant { Code = L2TenantCode });
                dbContext.Tenants.Should().NotContain(t => t.Code == L2TenantCode);
            }
        }
    }
}
