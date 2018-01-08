
namespace Sierra.Actor
{
    using System;
    using System.IO;
    using System.Net;
    using System.Linq;
    using System.Text;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using System.Fabric;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;
    using Microsoft.Azure.KeyVault;

    [StatePersistence(StatePersistence.Volatile)]
    internal class ForkActor : Actor, IForkActor
    {
        private Config _config;

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

            var accessToken = await ObtainVstsAccessToken();
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

            var client = GetHttpClient(accessToken);
            var response = await client.PostAsync($"{_config.VstsApiBaseUrl}/git/repositories?api-version=4.1-preview",
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

        // ReSharper disable once InconsistentNaming
        private async Task<string> GetKVAccessToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(_config.KeyVaultUClientId,
                _config.KeyVaultClientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        private async Task<string> ObtainVstsAccessToken()
        {
            //obtain refresh token from KV
            var kvClient = new KeyVaultClient(new KeyVaultCredential(GetKVAccessToken), new HttpClient());
            var vstsRefreshToken = await kvClient.GetSecretAsync(_config.KeyVaultUrl, "RefreshToken");
            //issue request to Vsts Token endpoint
            var newToken = await PerformTokenRequest(GenerateRefreshPostData(vstsRefreshToken.Value),
                _config.VstsTokenEndpoint);
            if (!String.IsNullOrWhiteSpace(newToken.RefreshToken) && !string.IsNullOrWhiteSpace(newToken.AccessToken))
            {
                //update KV with new refresh token       
                await kvClient.SetSecretAsync(_config.KeyVaultUrl, "RefreshToken", newToken.RefreshToken);
                return newToken.AccessToken;
            }

            throw new ForkingException("Missing access/refresh token from vsts endpoint");
        }


        private async Task<TokenModel> PerformTokenRequest(String postData, string tokenUrl)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(
                tokenUrl
            );

            webRequest.Method = "POST";
            webRequest.ContentLength = postData.Length;
            webRequest.ContentType = "application/x-www-form-urlencoded";

            using (StreamWriter swRequestWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                swRequestWriter.Write(postData);
            }

            try
            {
                HttpWebResponse hwrWebResponse = (HttpWebResponse)await webRequest.GetResponseAsync();

                if (hwrWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    string strResponseData;
                    using (StreamReader srResponseReader = new StreamReader(hwrWebResponse.GetResponseStream() ?? throw new ForkingException("Unexpected vsts response stream")))
                    {
                        strResponseData = srResponseReader.ReadToEnd();
                    }

                    return JsonConvert.DeserializeObject<TokenModel>(strResponseData);
                }

                throw new ForkingException($"token vsts endpoint returned {hwrWebResponse.StatusCode.ToString()}");
            }
            catch (Exception ex)
            {
                return await Task.FromException<TokenModel>(ex);
            }
        }

        private async Task<ListRepoResponse> ListRepos(string accessToken)
        {
            var client = GetHttpClient(accessToken);
            ListRepoResponse repos = null;

            var response = await client.GetAsync($"{_config.VstsApiBaseUrl}/git/repositories?api-version=4.1-preview");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                repos = JsonConvert.DeserializeObject<ListRepoResponse>(await response.Content.ReadAsStringAsync());
            }
            return repos;

        }

        private HttpClient GetHttpClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        private string GenerateRefreshPostData(string refreshToken)
        {
            return string.Format("client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={0}&grant_type=refresh_token&assertion={1}&redirect_uri={2}",
                WebUtility.UrlEncode(_config.VstsAppSecret),
                WebUtility.UrlEncode(refreshToken),
                _config.VstsOAuthCallbackUrl
            );

        }

        private void LoadConfiguration()
        {
            ConfigurationPackage configPackage = ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            KeyedCollection<string, ConfigurationProperty> forkSettings = configPackage.Settings.Sections["ForkActorAppConfigSection"].Parameters;
            Config newConfig = new Config();
            newConfig.VstsTokenEndpoint = forkSettings["VstsTokenEndpoint"].Value;
            newConfig.KeyVaultUrl = forkSettings["KeyVaultUrl"].Value;
            newConfig.KeyVaultUClientId = forkSettings["KeyVaultUClientId"].Value;
            newConfig.KeyVaultClientSecret = forkSettings["KeyVaultClientSecret"].Value;
            newConfig.VstsAppSecret = forkSettings["VstsAppSecret"].Value;
            newConfig.VstsOAuthCallbackUrl = forkSettings["VstsOAuthCallbackUrl"].Value;
            newConfig.VstsCollectionId = forkSettings["VstsCollectionId"].Value;
            newConfig.VstsTargetProjectId = forkSettings["VstsTargetProjectId"].Value;
            newConfig.VstsApiBaseUrl = forkSettings["VstsApiBaseUrl"].Value;

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
        private class TokenModel
        {

            [JsonProperty(PropertyName = "access_token")]
            internal String AccessToken { get; set; }

            [JsonProperty(PropertyName = "token_type")]
            internal String TokenType { get; set; }

            [JsonProperty(PropertyName = "expires_in")]
            internal String ExpiresIn { get; set; }

            [JsonProperty(PropertyName = "refresh_token")]
            internal String RefreshToken { get; set; }

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
