namespace Sierra.Common.DependencyInjection
{
    using System;
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Microsoft.TeamFoundation.Build.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;

    /// <summary>
    /// Sierra autofac module to setup all the Vsts DI chain.
    /// </summary>
    public class VstsModule : Module
    {      
        /// <summary>
        /// Adds registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be registered.</param>
        /// <remarks>
        /// Note that the ContainerBuilder parameter is unique to this module.
        /// </remarks>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c=> {
                var vstsConfig = new VstsConfiguration();
                c.Resolve<IConfigurationRoot>().Bind(vstsConfig);
                return vstsConfig;
            })
            .SingleInstance();

            builder.Register(c => new VssBasicCredential(string.Empty, c.Resolve<VstsConfiguration>().VstsPat))
                   .SingleInstance();

            builder.Register(c => new VssConnection(new Uri(c.Resolve<VstsConfiguration>().VstsBaseUrl), c.Resolve<VssBasicCredential>()))
                   .InstancePerDependency();

            builder.Register(c => c.Resolve<VssConnection>().GetClient<GitHttpClient>())
                   .InstancePerDependency();

            builder.Register(c => c.Resolve<VssConnection>().GetClient<BuildHttpClient>())
                    .InstancePerDependency();
        }
    }
}
