namespace Sierra.Common.DependencyInjection
{
    using System;
    using Autofac;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;

    /// <summary>
    /// Sierra autofac module to setup all the Vsts DI chain.
    /// </summary>
    public class VstsModule : Module
    {
        /// <summary>
        /// Get and sets the full URI for the keyvault to use in this module.
        /// </summary>
        public string Vault { get; set; }

        /// <summary>
        /// Adds registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be registered.</param>
        /// <remarks>
        /// Note that the ContainerBuilder parameter is unique to this module.
        /// </remarks>
        protected override void Load(ContainerBuilder builder)
        {
            var configBuilder = new ConfigurationBuilder().AddAzureKeyVault(
                Vault,
                new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                new DefaultKeyVaultSecretManager());

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
        }
    }
}
