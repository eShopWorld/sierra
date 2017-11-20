namespace Sierra.Api
{
    using System.IO;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder().UseKestrel()
                                           .UseContentRoot(Directory.GetCurrentDirectory())
                                           .ConfigureAppConfiguration((context, config) =>
                                           {
                                               config.AddJsonFile("appsettings.json")
                                                     .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                                           })
                                           .UseAzureAppServices()
                                           .ConfigureServices(services => services.AddAutofac())
                                           .UseStartup<Startup>()
                                           .Build();

            host.Run();
        }
    }
}
