namespace Sierra.Actor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Integration.ServiceFabric;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    internal static class Program
    {
        /// <summary>
        /// The entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            try
            {
                var configBuilder = new ConfigurationBuilder().AddAzureKeyVault(
                    @"https://esw-tooling-ci.vault.azure.net/",
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                    new DefaultKeyVaultSecretManager());

                var builder = new ContainerBuilder();
                builder.RegisterServiceFabricSupport();

                builder.RegisterInstance(configBuilder.Build())
                       .As<IConfigurationRoot>()
                       .SingleInstance();

                builder.RegisterActor<TenantActor>();
                builder.RegisterActor<LockerActor>();
                builder.RegisterActor<ForkActor>();

                //using (builder.Build())
                //{
                //    await Task.Delay(Timeout.Infinite);
                //}

                var ctx = builder.Build();
                var foo = ctx.Resolve<IConfigurationRoot>();

                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
