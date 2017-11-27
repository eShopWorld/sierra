namespace Sierra.Api
{
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

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(SierraVersion.LatestApi, new Info { Title = "Sierra Api", Version = SierraVersion.Sierra });
                var filePath = Path.Combine(Assembly.GetExecutingAssembly().Location, "Sierra.Api.xml");

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

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // autofac stuff
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
