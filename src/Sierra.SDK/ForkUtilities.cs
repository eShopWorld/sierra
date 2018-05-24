namespace Sierra.Common
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;

    public static class ForkUtilities
    {
        private const string ApiVersion = "5.0-preview.1";

        internal static async Task<VstsConfiguration> LoadVstsConfiguration(string keyVaultUrl, string clientId, string clientSecret)
        {
            //config loaded from settings, now proceed to stage 2- key vault
            //TODO:there does not seem to be KV API to read all secrets WITH values (last version), review this
            var kvClient = GetKeyVaultClient(clientId, clientSecret);
            var vstsTokenEndpointSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsTokenEndpoint");
            var vstsApiBaseUrlSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsApiBaseUrl");
            var vstsOAuthCallbackUrlSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsOAuthCallbackUrl");
            var vstsCollectionIdSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsCollectionId");
            var vstsTargetProjectIdSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsTargetProjectId");
            var vstsAppSecretSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsAppSecret");

            return new VstsConfiguration
            {
                VstsTokenEndpoint = vstsTokenEndpointSecret.Value,
                VstsApiBaseUrl = vstsApiBaseUrlSecret.Value,
                VstsAppSecret = vstsAppSecretSecret.Value,
                VstsCollectionId = vstsCollectionIdSecret.Value,
                VstsOAuthCallbackUrl = vstsOAuthCallbackUrlSecret.Value,
                VstsTargetProjectId = vstsTargetProjectIdSecret.Value
            };
        }

        internal static async Task<string> ObtainVstsAccessToken(string keyVaultUrl, string keyVaultClientId, string keyVaultClientSecret, VstsConfiguration vstsConfiguration)
        {
            //obtain refresh token from KV
            var kvClient = GetKeyVaultClient(keyVaultClientId, keyVaultClientSecret);

            var vstsRefreshToken = await kvClient.GetSecretAsync(keyVaultUrl, "RefreshToken");
            //issue request to Vsts Token endpoint
            var newToken = await PerformTokenRequest(GenerateRefreshPostData(vstsRefreshToken.Value, vstsConfiguration),
                vstsConfiguration.VstsTokenEndpoint);

            if (!string.IsNullOrWhiteSpace(newToken.RefreshToken) && !string.IsNullOrWhiteSpace(newToken.AccessToken))
            {
                //update KV with new refresh token       
                await kvClient.SetSecretAsync(keyVaultUrl, "RefreshToken", newToken.RefreshToken);
                return newToken.AccessToken;
            }

            throw new Exception("Missing access/refresh token from vsts endpoint");
        }


        internal static async Task<ListRepoResponse> ListRepos(string accessToken, VstsConfiguration vstsConfig)
        {
            var client = GetHttpClient(accessToken);
            ListRepoResponse repos = null;

            var response = await client.GetAsync($"{vstsConfig.VstsApiBaseUrl}/git/repositories?api-version={ApiVersion}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                repos = JsonConvert.DeserializeObject<ListRepoResponse>(await response.Content.ReadAsStringAsync());
            }
            return repos;
        }

        internal static async Task DeleteRepo(string accessToken, string repoName, VstsConfiguration vstsConfig)
        {
            var client = GetHttpClient(accessToken);
            var response =
                await client.DeleteAsync(
                    $"{vstsConfig.VstsApiBaseUrl}/git/repositories/{repoName}?api-version={ApiVersion}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to delete repository - {repoName}");
        }

        internal static async Task CreateFork(string forkSuffix, ListRepoResponseSingleRepo sourceRepo, string accessToken, VstsConfiguration vstsConfig)
        {
            var request = new ForkRequest
            {
                Name = $"{sourceRepo.Name}-{forkSuffix}",
                Project = new IdVstsWrapper { Id = vstsConfig.VstsTargetProjectId },
                ParentRepository = new ForkRequestParentRepository
                {
                    Id = sourceRepo.Id,
                    Project = new IdVstsWrapper
                    {
                        Id = sourceRepo.Project.Id
                    },
                    Collection = new IdVstsWrapper
                    {
                        Id = vstsConfig.VstsCollectionId
                    }
                }
            };

            var client = GetHttpClient(accessToken);
            var response = await client.PostAsync($"{vstsConfig.VstsApiBaseUrl}/git/repositories?api-version={ApiVersion}",
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fork a repo - response code - {response.StatusCode.ToString()}");
        }

        private static KeyVaultClient GetKeyVaultClient(string clientId, string clientSecret)
        {
            var kvClient = new KeyVaultClient(new KeyVaultCredential(async (authority, resource, scope) =>
            {
                var authContext = new AuthenticationContext(authority);
                ClientCredential clientCred = new ClientCredential(clientId,
                    clientSecret);

                AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

                if (result == null)
                    throw new InvalidOperationException("Failed to obtain the JWT token");

                return result.AccessToken;
            }), new HttpClient());
            return kvClient;
        }

        private static string GenerateRefreshPostData(string refreshToken, VstsConfiguration vstsConfiguration)
        {
            return string.Format("client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={0}&grant_type=refresh_token&assertion={1}&redirect_uri={2}",
                WebUtility.UrlEncode(vstsConfiguration.VstsAppSecret),
                WebUtility.UrlEncode(refreshToken),
                vstsConfiguration.VstsOAuthCallbackUrl
            );

        }

        private static async Task<TokenModel> PerformTokenRequest(String postData, string tokenUrl)
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
                    using (StreamReader srResponseReader = new StreamReader(hwrWebResponse.GetResponseStream() ?? throw new Exception("Unexpected vsts response stream")))
                    {
                        strResponseData = srResponseReader.ReadToEnd();
                    }

                    return JsonConvert.DeserializeObject<TokenModel>(strResponseData);
                }

                throw new Exception($"token vsts endpoint returned {hwrWebResponse.StatusCode.ToString()}");
            }
            catch (Exception ex)
            {
                return await Task.FromException<TokenModel>(ex);
            }
        }

        internal static HttpClient GetHttpClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        /// to be replaced once vsts client package is available under .net core
        internal class ForkRequest
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal IdVstsWrapper Project { get; set; }
            [JsonProperty(PropertyName = "parentRepository")]
            internal ForkRequestParentRepository ParentRepository { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class ForkRequestParentRepository : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "collection")]
            internal IdVstsWrapper Collection { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal IdVstsWrapper Project { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class TokenModel
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

        internal class VstsConfiguration
        {
            public string VstsTokenEndpoint { get; set; }
            public string VstsAppSecret { get; set; }
            public string VstsOAuthCallbackUrl { get; set; }
            public string VstsCollectionId { get; set; }
            public string VstsTargetProjectId { get; set; }
            public string VstsApiBaseUrl { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class ListRepoResponse
        {
            [JsonProperty(PropertyName = "value")]
            internal ListRepoResponseSingleRepo[] Value { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class ListRepoResponseSingleRepo : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
            [JsonProperty(PropertyName = "project")]
            internal ListRepoResponseProjectRef Project { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class ListRepoResponseProjectRef : IdVstsWrapper
        {
            [JsonProperty(PropertyName = "name")]
            internal string Name { get; set; }
        }

        /// to be replaced once vsts client package is available under .net core
        internal class IdVstsWrapper
        {
            [JsonProperty(PropertyName = "id")]
            internal string Id { get; set; }
        }
    }
}
