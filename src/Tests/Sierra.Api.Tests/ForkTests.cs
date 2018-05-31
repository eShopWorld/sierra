//using System;
//using System.Net;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using Eshopworld.DevOps;
//using Eshopworld.Tests.Core;
//using FluentAssertions;
//using IdentityModel.Client;
//using Sierra.Common;
//using Xunit;

//// ReSharper disable once CheckNamespace
//public class ForkTests
//{
//    private Settings Settings { get; set; }

//    private VstsConfiguration VstsConfig { get; set; }

//    [Fact, IsIntegration]
//    public async void CreateForkTest()
//    {
//        await SetConfiguration();
//        HttpClient client = new HttpClient();
//        var suffix = Guid.NewGuid().ToString();

//        //obtain access token
//        var stsAccessToken = await ObtainSTSAccessToken();
//        client.SetBearerToken(stsAccessToken);

//        //issue fork request
//        var respo = await client.PostAsync(Settings.ApiUrl,
//            new StringContent(
//                $"{{\"sourceRepositoryName\": \"ForkIntTestSourceRepo\",\"forkSuffix\": \"{suffix}\"}}", Encoding.UTF8, "application/json"));

//        respo.StatusCode.Should().Be(HttpStatusCode.OK);

//        //list repos
//        var vstsConfig = await ForkUtilities.LoadVstsConfiguration(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
//            Settings.KeyVaultClientSecret);

//        var vstsAccessToken = await ForkUtilities.ObtainVstsAccessToken(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
//            Settings.KeyVaultClientSecret, vstsConfig);

//        var repoName = $"ForkIntTestSourceRepo-{suffix}";

//        var repos = await ForkUtilities.ListRepos(vstsAccessToken, vstsConfig);
//        var repo = repos.FirstOrDefault(i => i.Name == repoName);
//        repo.Should().NotBeNull();

//        //delete the repo
//        await ForkUtilities.DeleteRepo(vstsAccessToken, repo.Id, vstsConfig);
//    }

//    private async Task SetConfiguration()
//    {
//        var config = EswDevOpsSdk.BuildConfiguration(true);
//        //TODO: review why binder was not working here
//        Settings = new Settings
//        {
//            ApiUrl = config["TestConfig:ApiUrl"],
//            KeyVaultUrl = config["TestConfig:KeyVaultUrl"],
//            KeyVaultClientSecret = config["TestConfig:KeyVaultClientSecret"],
//            KeyVaultClientId = config["TestConfig:KeyVaultClientId"],
//            STSAuthority = config["TestConfig:STSAuthority"],
//            STSClientSecret = config["TestConfig:STSClientSecret"],
//            STSClientId = config["TestConfig:STSClientId"],
//            STSScope = config["TestConfig:STSScope"]
//        };

//        VstsConfig = await ForkUtilities.LoadVstsConfiguration(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
//            Settings.KeyVaultClientSecret);
//    }

//    // ReSharper disable once InconsistentNaming
//    private async Task<string> ObtainSTSAccessToken()
//    {
//        var discovery = await DiscoveryClient.GetAsync(Settings.STSAuthority);
//        var client = new TokenClient(discovery.TokenEndpoint, Settings.STSClientId, Settings.STSClientSecret);

//        var tokenResponse = await client.RequestClientCredentialsAsync(Settings.STSScope);
//        return tokenResponse.AccessToken;
//    }
//}

//internal class Settings
//{
//    internal string ApiUrl { get; set; }
//    internal string KeyVaultUrl { get; set; }
//    internal string KeyVaultClientId { get; set; }
//    internal string KeyVaultClientSecret { get; set; }
//    internal string STSAuthority { get; set; }
//    internal string STSClientId { get; set; }
//    internal string STSClientSecret { get; set; }
//    internal string STSScope { get; set; }
//}
