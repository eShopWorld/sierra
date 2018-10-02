namespace Sierra.Api
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.IO;
    using System.Reflection;
    using Autofac;
    using Common.DependencyInjection;
    using Eshopworld.Core;
    using Eshopworld.DevOps;
    using Eshopworld.Telemetry;
    using Eshopworld.Web;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Model;
    using Swashbuckle.AspNetCore.Swagger;

    /// <summary>
    /// Startup entry point for the Sierra.Api fabric service.
    /// </summary>
    public class Startup
    {
        // TODO: Review BB code after fixing its extension methods
        private readonly BigBrother _bb;
        private readonly IConfigurationRoot _configuration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="env">hosting environment</param>
        public Startup(IHostingEnvironment env)
        {
            try
            {
                _configuration = EswDevOpsSdk.BuildConfiguration(env.ContentRootPath, env.EnvironmentName);
                var internalKey = _configuration["BBInstrumentationKey"];
                _bb = new BigBrother(internalKey, internalKey);
                _bb.UseEventSourceSink().ForExceptions();
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }

        /// <summary>
        /// The framework service configuration entry point.
        ///     Do not use this to setup anything without a <see cref="IServiceCollection"/> extension method!
        /// </summary>
        /// <param name="services">The contract for a collection of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddApplicationInsightsTelemetry(_configuration);
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc(SierraVersion.LatestApi, new Info { Title = "Sierra Api", Version = SierraVersion.Sierra });
                    c.AddSecurityDefinition("Bearer",
                        new ApiKeyScheme
                        {
                            In = "header",
                            Description = "Please insert JWT with Bearer into field",
                            Name = "Authorization",
                            Type = "apiKey"
                        });
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

                services.AddAuthorization(options =>
                {
                    options.AddPolicy("AssertScope", policy =>
                        policy.RequireClaim("scope", "esw.sierra.api.all"));
                });

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddIdentityServerAuthentication(x =>
                {
                    x.ApiName = _configuration["STSConfig:ApiName"];
                    x.Authority = _configuration["STSConfig:Authority"];
                });

                services.AddMvc(options =>
                {
                    var policy = ScopePolicy.Create("esw.sierra.api.all");
                    options.Filters.Add(new AuthorizeFilter(policy));
                });
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                _bb.Flush();
                throw;
            }
        }

        /// <summary>
        /// The framework DI container configuration entry point.
        ///     Use this to setup specific AutoFac dependencies that don't have <see cref="IServiceCollection"/> extension methods.
        /// </summary>
        /// <param name="builder">The builder for an <see cref="T:Autofac.IContainer" /> from component registrations.</param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
        }

        /// <summary>
        /// The framework HTTP request pipeline configuration entry point.
        /// </summary>
        /// <param name="app">The mechanisms to configure an application's request pipeline.</param>
        /// <param name="env">The information about the web hosting environment an application is running in.</param>
        /// <param name="statelessServiceContext">The context of Service Fabric stateless service.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, StatelessServiceContext statelessServiceContext)
        {
            try
            {
                if (Debugger.IsAttached) app.UseDeveloperExceptionPage();
                app.UseBigBrotherExceptionHandler();
                app.UseAuthentication();
                if (_configuration.GetValue<bool>("ActorDirectCallMiddlewareEnabled"))
                {
                    app.UseActorDirectCall(new ActorDirectCallOptions
                    {
                        StatelessServiceContext = statelessServiceContext,
                    });
                }

                app.UseMvc();

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint($"/swagger/{SierraVersion.LatestApi}/swagger.json", $"Sierra Api {SierraVersion.LatestApi}");
                });
            }
            catch (Exception e)
            {
                _bb.Publish(e.ToExceptionEvent());
                _bb.Flush();
                throw;
            }
        }
    }
}
