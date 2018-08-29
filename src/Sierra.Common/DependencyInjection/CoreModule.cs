namespace Sierra.Common.DependencyInjection
{
    using Autofac;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Eshopworld.Telemetry;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    /// <summary>
    /// some key  - devops + runtime -  level services are registered here
    /// </summary>
    public class CoreModule : Module
    {
        /// <summary>
        /// Get and sets the full URI for the keyvault to use in this module.
        /// </summary>
        public string Vault { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var configBuilder = new ConfigurationBuilder().AddAzureKeyVault(
                Vault,
                new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                new SectionKeyVaultManager());

            var config = configBuilder.Build();

            builder.RegisterInstance(config)
                   .As<IConfigurationRoot>()
                   .SingleInstance();

            builder.Register<IBigBrother>(c =>
            {
                var insKey = c.Resolve<IConfigurationRoot>()["BBInstrumentationKey"];
                return new BigBrother(insKey, insKey);
            })
            .SingleInstance();
        }
    }
}
