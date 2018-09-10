namespace Sierra.Common.DependencyInjection
{
    using Autofac;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Eshopworld.Telemetry;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// some key  - devops + runtime -  level services are registered here
    /// </summary>
    public class CoreModule : Module
    {
        private bool TestMode { get; }

        /// <summary>
        /// core module constructor allowing to enable test mode - disabled by default
        /// </summary>
        /// <param name="testMode">test mode flag</param>
        public CoreModule(bool testMode = false)
        {
            TestMode = testMode;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var config = EswDevOpsSdk.BuildConfiguration(TestMode);

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
