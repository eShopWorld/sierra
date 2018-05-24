using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Sierra.Common;
using Xunit;

namespace Sierra.Api.Tests
{
    public class ForkIntTests
    {
        private Settings Settings { get; set; }
        private ForkUtilities.VstsConfiguration VstsConfig { get; set; }

        [Fact, IsIntegration]
        public async void CreateForkTest()
        {
            await SetConfiguration();
            HttpClient client = new HttpClient();
            var suffix = Guid.NewGuid().ToString();

            //issue fork request
            var respo = await client.PostAsync(Settings.ApiUrl,
                new StringContent(
                    $"{{\"sourceRepositoryName\": \"ForkIntTestSourceRepo\",\"forkSuffix\": \"{suffix}\"}}", Encoding.UTF8, "application/json"));
            respo.StatusCode.Should().Be(HttpStatusCode.OK);

            //list repos
            var vstsConfig = await ForkUtilities.LoadVstsConfiguration(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
                Settings.KeyVaultClientSecret);

            var accessToken = await ForkUtilities.ObtainVstsAccessToken(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
                Settings.KeyVaultClientSecret, vstsConfig);

            var repoName = $"ForkIntTestSourceRepo-{suffix}";

            var repos = await ForkUtilities.ListRepos(accessToken, vstsConfig);
            var repo = repos.Value.FirstOrDefault(i => i.Name == repoName);
            repo.Should().NotBeNull();

            //delete the repo
            await ForkUtilities.DeleteRepo(accessToken, repo.Id, vstsConfig);
        }

        private async Task SetConfiguration()
        {          
            var config = EswDevOpsSdk.BuildConfiguration(true);
            //TODO: review why binder was not working here
            Settings = new Settings
            {
                ApiUrl = config["TestConfig:ApiUrl"],
                KeyVaultUrl = config["TestConfig:KeyVaultUrl"],
                KeyVaultClientSecret = config["TestConfig:KeyVaultClientSecret"],
                KeyVaultClientId = config["TestConfig:KeyVaultClientId"]
            };

            VstsConfig = await ForkUtilities.LoadVstsConfiguration(Settings.KeyVaultUrl, Settings.KeyVaultClientId,
                Settings.KeyVaultClientSecret);
        }
    }

    internal class Settings
    {
        internal string ApiUrl { get; set; }
        internal string KeyVaultUrl { get; set; }
        internal string KeyVaultClientId { get; set; }
        internal string KeyVaultClientSecret { get; set; }
    }
}
