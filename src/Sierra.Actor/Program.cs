namespace Sierra.Actor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Integration.ServiceFabric;
    using Common;
    using Eshopworld.Telemetry;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;

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
                var config = configBuilder.Build();

                builder.RegisterInstance(config)
                       .As<IConfigurationRoot>()
                       .SingleInstance();

                var vstsConfig = new VstsConfiguration();
                config.Bind(vstsConfig);

                builder.RegisterInstance(vstsConfig)
                       .SingleInstance();

                builder.Register(c => new VssBasicCredential(string.Empty, c.Resolve<VstsConfiguration>().VstsPat))
                       .SingleInstance();

                builder.Register(c => new VssConnection(new Uri(c.Resolve<VstsConfiguration>().VstsBaseUrl), c.Resolve<VssBasicCredential>()))
                       .InstancePerDependency();

                builder.Register(c => c.Resolve<VssConnection>().GetClient<GitHttpClient>())
                       .InstancePerDependency();

                builder.RegisterActor<TenantActor>();
                builder.RegisterActor<LockerActor>();
                builder.RegisterActor<ForkActor>();

                //using (builder.Build())
                //{
                //    await Task.Delay(Timeout.Infinite);
                //}

                var ctx = builder.Build();
                var foo = ctx.Resolve<GitHttpClient>();

                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }
    }
}
