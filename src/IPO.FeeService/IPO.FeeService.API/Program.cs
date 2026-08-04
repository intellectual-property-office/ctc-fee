using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using IPO.Common.API;
using IPO.Configuration;

namespace IPO.FeeService.API
{
    public class Program
    {
        [ExcludeFromCodeCoverage]
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        [ExcludeFromCodeCoverage]
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(configBuilder =>
                    {
                        // Add IPO Azure Configuration - only added when Azure App Config is set
                        configBuilder.AddIPOAzureAppConfigWithManagedIdentity();

                        // Add Template replacement configuration provider to replace templated values
                        configBuilder.AddTemplateConfiguration();
                    });
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    var configuration = context.Configuration;
                    LoggingConfigurator.ConfigureLogging(configuration, logging);
                });
    }
}