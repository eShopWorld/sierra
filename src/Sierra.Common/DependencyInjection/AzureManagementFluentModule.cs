namespace Sierra.Common.DependencyInjection
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Eshopworld.DevOps;
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
                return new TokenCredentials(tokenProviderAdapter);
            });

            builder.Register(c =>
            {
                var tokenCredentials = c.Resolve<TokenCredentials>();

                var client = RestClient.Configure()
                    .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty,
                        AzureEnvironment.AzureGlobalCloud))
                    .Build();

                return Azure.Authenticate(client, string.Empty);
            });

            foreach (DeploymentEnvironment env in Enum.GetValues(typeof(DeploymentEnvironment)))
            {
                var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(env);
                builder.Register(c =>
                {
                    var authenticated = c.Resolve<Azure.IAuthenticated>();
                    return authenticated.WithSubscription(subscriptionId);
                }).Keyed<IAzure>(env).InstancePerLifetimeScope();
            }
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
