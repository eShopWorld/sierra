namespace Sierra.Api
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Autofac;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Model;
    using Swashbuckle.AspNetCore.Swagger;

    /// <summary>
    /// Startup entry point for the Sierra.Api fabric service.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Startup"/>.
        /// </summary>
        /// <param name="configuration">[Injected] The set of key/value application configuration properties.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets and sets the set of key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// The framework service configuration entry point.
        ///     Do not use this to setup anything without a <see cref="IServiceCollection"/> extension method!
        /// </summary>
        /// <param name="services">The contract for a collection of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(SierraVersion.LatestApi, new Info { Title = "Sierra Api", Version = SierraVersion.Sierra });
                var filePath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Wrong check for the swagger XML file! 'Assembly.GetExecutingAssembly().Location' came back null!"),
                    "Sierra.Api.xml");

                if (File.Exists(filePath))
                {
                    c.IncludeXmlComments(filePath);
                }
                else
                {
                    if (Debugger.IsAttached)
                    {
                        // Couldn't find the XML file! check that XML comments are being built and that the file name checks
                        Debugger.Break();
                    }
                }
            });
        }

        /// <summary>
        /// The framework DI container configuration entry point.
        ///     Use this to setup specific autofac dependencies that don't have <see cref="IServiceCollection"/> extension methods.
        /// </summary>
        /// <param name="builder">The builder for an <see cref="T:Autofac.IContainer" /> from component registrations.</param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // autofac stuff
        }

        /// <summary>
        /// The framework HTTP request pipeline configuration entry point.
        /// </summary>
        /// <param name="app">The mechanisms to configure an application's request pipeline.</param>
        /// <param name="env">The information about the web hosting environment an application is running in.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
            if (Debugger.IsAttached) app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{SierraVersion.LatestApi}/swagger.json", $"Sierra Api {SierraVersion.LatestApi}");
            });
        }
    }
}
