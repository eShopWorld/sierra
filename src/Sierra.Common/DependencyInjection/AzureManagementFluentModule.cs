namespace Sierra.Common.DependencyInjection
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Rest;

    public class AzureManagementFluentModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var tokenProvider = new AzureServiceTokenProvider();
                var tokenProviderAdapter = new AzureServiceTokenProviderAdapter(tokenProvider);
                var tokenCredentials = new TokenCredentials(tokenProviderAdapter);

                var client = RestClient.Configure()
                    .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty,
                        AzureEnvironment.AzureGlobalCloud))
                    .Build();

                // TODO: per subscription cache or a pool could be used here
                return Azure.Authenticate(client, string.Empty);
            });
        }

        private class AzureServiceTokenProviderAdapter : ITokenProvider
        {
            private const string Bearer = "Bearer";
            private readonly AzureServiceTokenProvider _azureTokenProvider;

            public AzureServiceTokenProviderAdapter(AzureServiceTokenProvider azureTokenProvider)
            {
                _azureTokenProvider = azureTokenProvider;
            }

            public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
            {
                var token = await _azureTokenProvider.GetAccessTokenAsync("https://management.core.windows.net/", string.Empty);
                return new AuthenticationHeaderValue(Bearer, token);
            }
        }
    }
}
