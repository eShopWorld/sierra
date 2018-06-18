using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Sierra.Api.Tests;
using Xunit;

// ReSharper disable once CheckNamespace
[Collection(nameof(ActorContainerCollection))]
public class ForkTests
{
    public readonly ActorContainerFixture ContainerFixture;

    public readonly TestConfig Config = new TestConfig();

    public ForkTests(ActorContainerFixture containerFixture)
    {
        ContainerFixture = containerFixture;
        var config = EswDevOpsSdk.BuildConfiguration(true);
        config.GetSection("TestConfig").Bind(Config);
    }

    [Fact, IsLayer2]
    public async void Create_Fork_API()
    {
        HttpClient client = new HttpClient();
        var suffix = Guid.NewGuid().ToString();

        //obtain access token
        var stsAccessToken = await ObtainSTSAccessToken();
        client.SetBearerToken(stsAccessToken);

        //issue fork request
        var respo = await client.PostAsync(
            Config.ApiUrl,
            new StringContent($"{{\"sourceRepositoryName\": \"ForkIntTestSourceRepo\",\"forkSuffix\": \"{suffix}\"}}", Encoding.UTF8, "application/json"));

        respo.StatusCode.Should().Be(HttpStatusCode.OK);

        //idempotency check
        respo = await client.PostAsync(
          Config.ApiUrl,
          new StringContent($"{{\"sourceRepositoryName\": \"ForkIntTestSourceRepo\",\"forkSuffix\": \"{suffix}\"}}", Encoding.UTF8, "application/json"));

        respo.StatusCode.Should().Be(HttpStatusCode.OK);

        //list repos
        using (var gitClient = ContainerFixture.Container.Resolve<GitHttpClient>())
        {
            var repo = (await gitClient.GetRepositoriesAsync()).SingleOrDefault(r => r.Name == $"ForkIntTestSourceRepo-{suffix}");
            repo.Should().NotBeNull();

            await gitClient.DeleteRepositoryAsync(repo.Id);
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<string> ObtainSTSAccessToken()
    {
        var discovery = await DiscoveryClient.GetAsync(Config.STSAuthority);
        var client = new TokenClient(discovery.TokenEndpoint, Config.STSClientId, Config.STSClientSecret);

        var tokenResponse = await client.RequestClientCredentialsAsync(Config.STSScope);
        return tokenResponse.AccessToken;
    }
}

public class TestConfig
{
    public string ApiUrl { get; set; }

    public string STSAuthority { get; set; }

    public string STSClientId { get; set; }

    public string STSClientSecret { get; set; }

    public string STSScope { get; set; }
}
