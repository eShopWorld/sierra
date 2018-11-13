namespace Sierra.Common.DependencyInjection
{
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
            // TODO: How to cache authentication token?
            builder.Register(c =>
            {
                var token = new AzureServiceTokenProvider()
                    .GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
                var tokenCredentials = new TokenCredentials(token);

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
    }
}
