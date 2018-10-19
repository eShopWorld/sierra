namespace Sierra.Common.DependencyInjection
{
    using System.Linq;
    using Autofac;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Sierra AutoFac module to setup environments-related DI chain.
    /// </summary>
    public class EnvironmentsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var subscriptions = c.Resolve<IConfigurationRoot>()
                    .GetSection("AzureSubscriptions")
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value);
                return new EnvironmentConfiguration(subscriptions);
            });
        }
    }
}
