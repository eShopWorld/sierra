using System.Net.Http.Headers;

namespace Sierra.Actor
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Newtonsoft.Json;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    //TODO: this could be easily a new package perhaps?
    internal static class Utilities
    {
        internal static  async Task<string> ObtainVstsAccessToken(AuthConfig config )
        {
            //obtain refresh token from KV
            var kvClient = new KeyVaultClient(new KeyVaultCredential( async (authority, resource, scope) =>
            {
                var authContext = new AuthenticationContext(authority);
                ClientCredential clientCred = new ClientCredential(config.KeyVaultClientId,
                    config.KeyVaultClientSecret);

                AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

                if (result == null)
                    throw new InvalidOperationException("Failed to obtain the JWT token");

                return result.AccessToken;
            }), new HttpClient());
          
            var vstsRefreshToken = await kvClient.GetSecretAsync(config.KeyVaultUrl, "RefreshToken");
            //issue request to Vsts Token endpoint
            var newToken = await PerformTokenRequest(GenerateRefreshPostData(vstsRefreshToken.Value, config),
                config.VstsTokenEndpoint);

            if (!string.IsNullOrWhiteSpace(newToken.RefreshToken) && !string.IsNullOrWhiteSpace(newToken.AccessToken))
            {
                //update KV with new refresh token       
                await kvClient.SetSecretAsync(config.KeyVaultUrl, "RefreshToken", newToken.RefreshToken);
                return newToken.AccessToken;
            }

            throw new Exception("Missing access/refresh token from vsts endpoint");
        }

        private static  string GenerateRefreshPostData(string refreshToken, AuthConfig config)
        {
            return string.Format("client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={0}&grant_type=refresh_token&assertion={1}&redirect_uri={2}",
                WebUtility.UrlEncode(config.VstsAppSecret),
                WebUtility.UrlEncode(refreshToken),
                config.VstsOAuthCallbackUrl
            );

        }

        internal static async Task<TokenModel> PerformTokenRequest(String postData, string tokenUrl)
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

        internal class AuthConfig
        {
            internal string KeyVaultUrl { get; set; }
            internal string VstsTokenEndpoint { get; set; }
            internal string KeyVaultClientId { get; set; }
            internal string KeyVaultClientSecret { get; set; }
            internal string VstsAppSecret { get; set; }
            internal string VstsOAuthCallbackUrl { get; set; }
        }
    }
}
