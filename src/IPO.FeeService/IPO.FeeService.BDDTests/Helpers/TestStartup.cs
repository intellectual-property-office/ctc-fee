using IPO.FeeService.API;
using IPO.FeeService.BDDTests.Mocks;
using IPO.FeeService.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IPO.FeeService.BDDTests.Helpers
{
    public class TestStartup : Startup
    {
        public static Common.API.Version? Version;
        public TestStartup(IConfiguration configuration) : base(configuration)
        {
            Version = Helper.Version;
        }
        protected override void MigrateDatabase(IApplicationBuilder app) { }
        protected override void SeedDatabase(IApplicationBuilder app, string contentRootPath) { }
        protected override void AddDatabase(IServiceCollection services) { }
        protected override void AddServices(IServiceCollection services)
        {
            services.AddScoped<IFeeManagementService, MockFeeManagementService>();
        }

        public static TestServer GetTestServer()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost
                        .UseTestServer()
                        .UseStartup<TestStartup>();
                });
            var host = hostBuilder.Start();
            return host.GetTestServer();
        }
    }
}
