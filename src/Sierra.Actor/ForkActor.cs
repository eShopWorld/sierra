
namespace Sierra.Actor
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Text;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using System.Fabric;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;

    [StatePersistence(StatePersistence.Volatile)]
    internal class ForkActor : Actor, IForkActor
    {
        private Config _config;

        private const string ApiVersion = "5.0-preview.1";

        public ForkActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
        }

        /// <summary>
        /// for the source repository
        /// </summary>
        /// <param name="fork">request payload</param>
        /// <returns>Task instance</returns>
        public async Task ForkRepo(Fork fork)
        {
            if (!ValidateRequest(fork))
                throw new ForkingException("Invalid message received");

            var accessToken = await Utilities.ObtainVstsAccessToken(
                new Utilities.AuthConfig //TODO: consider using "auto mapper"
                {
                    KeyVaultUrl = _config.KeyVaultUrl,
                    VstsTokenEndpoint = _config.VstsTokenEndpoint,
                    KeyVaultClientId =  _config.KeyVaultUClientId,
                    KeyVaultClientSecret = _config.KeyVaultClientSecret,
                    VstsAppSecret =  _config.VstsAppSecret,
                    VstsOAuthCallbackUrl = _config.VstsOAuthCallbackUrl
                });

            var repos = await ListRepos(accessToken);
            var sourceRepo = repos.Value.FirstOrDefault(i => i.Name == fork.SourceRepositoryName);
            if (sourceRepo == null)
                throw new ForkingException($"Repository {fork.SourceRepositoryName} not found");

            await CreateFork(fork, sourceRepo, accessToken);
        }

        private async Task CreateFork(Fork fork, ListRepoResponseSingleRepo sourceRepo, string accessToken)
        {
            var request = new ForkRequest
            {
                Name = $"{sourceRepo.Name}-{fork.ForkSuffix}",
                Project = new IdVstsWrapper { Id = _config.VstsTargetProjectId },
                ParentRepository = new ForkRequestParentRepository
                {
                    Id = sourceRepo.Id,
                    Project = new IdVstsWrapper
                    {
                        Id = sourceRepo.Project.Id
                    },
                    Collection = new IdVstsWrapper
                    {
                        Id = _config.VstsCollectionId
                    }
                }
            };

            var client = Utilities.GetHttpClient(accessToken);
            var response = await client.PostAsync($"{_config.VstsApiBaseUrl}/git/repositories?api-version={ApiVersion}",
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                throw new ForkingException($"Failed to fork a repo - response code - {response.StatusCode.ToString()}");
        }

        private static bool ValidateRequest(Fork fork)
        {
            return !string.IsNullOrWhiteSpace(fork?.SourceRepositoryName) && !string.IsNullOrWhiteSpace(fork.ForkSuffix);
        }

        protected override Task OnActivateAsync()
        {
            LoadConfiguration();
            return Task.CompletedTask;
        }


        private async Task<ListRepoResponse> ListRepos(string accessToken)
        {
            var client = Utilities.GetHttpClient(accessToken);
            ListRepoResponse repos = null;

            var response = await client.GetAsync($"{_config.VstsApiBaseUrl}/git/repositories?api-version={ApiVersion}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                repos = JsonConvert.DeserializeObject<ListRepoResponse>(await response.Content.ReadAsStringAsync());
            }
            return repos;

        }

        private void LoadConfiguration()
        {
            ConfigurationPackage configPackage = ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            KeyedCollection<string, ConfigurationProperty> forkSettings = configPackage.Settings.Sections["ForkActorAppConfigSection"].Parameters;
            Config newConfig = new Config
            {
                VstsTokenEndpoint = forkSettings["VstsTokenEndpoint"].Value,
                KeyVaultUrl = forkSettings["KeyVaultUrl"].Value,
                KeyVaultUClientId = forkSettings["KeyVaultClientId"].Value,
                KeyVaultClientSecret = forkSettings["KeyVaultClientSecret"].Value,
                VstsAppSecret = forkSettings["VstsAppSecret"].Value,
                VstsOAuthCallbackUrl = forkSettings["VstsOAuthCallbackUrl"].Value,
                VstsCollectionId = forkSettings["VstsCollectionId"].Value,
                VstsTargetProjectId = forkSettings["VstsTargetProjectId"].Value,
                VstsApiBaseUrl = forkSettings["VstsApiBaseUrl"].Value
            };

            _config = newConfig;
        }

        private class Config
        {
            internal string VstsTokenEndpoint { get; set; }
            internal string KeyVaultUrl { get; set; }
            internal string KeyVaultUClientId { get; set; }
            internal string KeyVaultClientSecret { get; set; }
            internal string VstsAppSecret { get; set; }
            internal string VstsOAuthCallbackUrl { get; set; }
            internal string VstsCollectionId { get; set; }
            internal string VstsTargetProjectId { get; set; }
            internal string VstsApiBaseUrl { get; set; }
        }

      

        /// to be replaced once vsts client package is available under .net core
        private class ListRepoResponse
        {
            [JsonProperty(PropertyName = "value")]
            internal ListRepoResponseSingleRepo[] Value { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        private class ListRepoResponseSingleRepo : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal ListRepoResponseProjectRef Project { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        private class ListRepoResponseProjectRef : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        private class ForkRequest
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal IdVstsWrapper Project { get; set; }
            [JsonProperty(PropertyName = "parentRepository")]
            internal ForkRequestParentRepository ParentRepository { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        private class ForkRequestParentRepository : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "collection")]
            internal IdVstsWrapper Collection { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal IdVstsWrapper Project { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        private class IdVstsWrapper
        {
            [JsonProperty(PropertyName = "id")]
            internal string Id { get; set; }
        }

        private class ForkingException : Exception
        {
            public ForkingException(string msg) : base(msg)
            {
            }
        }
    }
}
