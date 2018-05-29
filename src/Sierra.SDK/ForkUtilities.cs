namespace Sierra.Common
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Azure.KeyVault;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.TeamFoundation.Core.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.OAuth;
    using Microsoft.VisualStudio.Services.WebApi;
    using Newtonsoft.Json;

    public static class ForkUtilities
    {
        private const string RefreshTokenSecretName = "RefreshToken";

        internal static async Task<VstsConfiguration> LoadVstsConfiguration(string keyVaultUrl, string clientId, string clientSecret)
        {
            //config loaded from settings, now proceed to stage 2- key vault
            //TODO:there does not seem to be KV API to read all secrets WITH values (last version), review this
            var kvClient = GetKeyVaultClient(clientId, clientSecret);
            var vstsTokenEndpointSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsTokenEndpoint");
            var vstsBaseUrlSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsBaseUrl");
            var vstsOAuthCallbackUrlSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsOAuthCallbackUrl");
            var vstsCollectionIdSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsCollectionId");
            var vstsTargetProjectIdSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsTargetProjectId");
            var vstsAppSecretSecret = await kvClient.GetSecretAsync(keyVaultUrl, "VstsAppSecret");

            return new VstsConfiguration
            {
                VstsTokenEndpoint = vstsTokenEndpointSecret.Value,
                VstsBaseUrl = vstsBaseUrlSecret.Value,
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

            var vstsRefreshToken = await kvClient.GetSecretAsync(keyVaultUrl, RefreshTokenSecretName);

            //issue request to Vsts Token endpoint
            var newToken = await PerformTokenRequest(GenerateRefreshPostData(vstsRefreshToken.Value, vstsConfiguration),
                vstsConfiguration.VstsTokenEndpoint);

            if (!string.IsNullOrWhiteSpace(newToken.RefreshToken) && !string.IsNullOrWhiteSpace(newToken.AccessToken))
            {
                //update KV with new refresh token       
                await kvClient.SetSecretAsync(keyVaultUrl, RefreshTokenSecretName, newToken.RefreshToken);
                return newToken.AccessToken;
            }

            throw new Exception("Missing access/refresh token from vsts endpoint");
        }


        internal static async Task<List<GitRepository>> ListRepos(string accessToken, VstsConfiguration vstsConfig)
        {
            var client = GetHttpClient(accessToken, vstsConfig.VstsBaseUrl);

            return await client.GetRepositoriesAsync();
        }

        internal static async Task DeleteRepo(string accessToken, Guid repoId, VstsConfiguration vstsConfig)
        {
            var client = GetHttpClient(accessToken, vstsConfig.VstsBaseUrl);
            await client.DeleteRepositoryAsync(repoId);
        }

        internal static async Task CreateFork(string forkSuffix, GitRepository sourceRepo, string accessToken, VstsConfiguration vstsConfig)
        {
            var client = GetHttpClient(accessToken, vstsConfig.VstsBaseUrl);
            await client.CreateRepositoryAsync(new GitRepositoryCreateOptions
            {
                Name = $"{sourceRepo.Name}-{forkSuffix}",
                ProjectReference = new TeamProjectReference { Id = Guid.Parse(vstsConfig.VstsTargetProjectId) },
                ParentRepository = new GitRepositoryRef
                {
                    Id = sourceRepo.Id,
                    ProjectReference = new TeamProjectReference { Id = Guid.Parse(vstsConfig.VstsTargetProjectId) },
                    Collection = new TeamProjectCollectionReference { Id = Guid.Parse(vstsConfig.VstsCollectionId) }
                }
            });

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
            return $"client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={WebUtility.UrlEncode(vstsConfiguration.VstsAppSecret)}&grant_type=refresh_token&assertion={WebUtility.UrlEncode(refreshToken)}&redirect_uri={vstsConfiguration.VstsOAuthCallbackUrl}";
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

            HttpWebResponse hwrWebResponse = (HttpWebResponse)await webRequest.GetResponseAsync();

            if (hwrWebResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception($"token vsts endpoint returned {hwrWebResponse.StatusCode.ToString()}");

            string strResponseData;
            using (StreamReader srResponseReader = new StreamReader(hwrWebResponse.GetResponseStream() ?? throw new Exception("Unexpected vsts response stream")))
            {
                strResponseData = srResponseReader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<TokenModel>(strResponseData);
        }

        private static GitHttpClient GetHttpClient(string accessToken, string vstsBaseUrl)
        {
            var connection = new VssConnection(new Uri($"{vstsBaseUrl}/DefaultCollection"), new VssCredentials(new VssOAuthAccessTokenCredential(accessToken)));

            return connection.GetClient<GitHttpClient>();
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
    }
}
