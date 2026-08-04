using IPO.Common.API;
using IPO.CTC.HealthChecks;
using IPO.FeeService.API.Serialisation;
using IPO.FeeService.Data;
using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API;
using IPO.FeeService.Services.Services;
using IPO.FeeService.Services.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json.Serialization;

namespace IPO.FeeService.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Helper = new IPOStartupHelper("IPO.FeeService.API", "version");
        }

        public IConfiguration Configuration { get; }

        public IPOStartupHelper Helper { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Helper.AddIPOServicesConfiguration(services, mvcBuilderAction: x => x.AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())));

            AddDatabase(services);
            AddHealthChecks(services);
            AddServices(services);
            AddDateFormatter(services);

            services.AddSwaggerGen(config =>
            {
                config.ExampleFilters();
                config.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Fee Calculation Microservice",
                    Version = "v1"
                });

                config.EnableAnnotations();
            });

            AddSwaggerExamplesForType<FeeCalculationRequestExamples>(services);
            AddSwaggerExamplesForType<FeeInformationRequestExamples>(services);
        }

        protected virtual void AddServices(IServiceCollection services)
        {
            services.AddIPOErrorAwareScoped<IFeeRepository, FeeDbRepository>("E003");
            services.AddIPOErrorAwareScoped<IFeeManagementService, FeeManagementService>("E005");
            services.AddIPOErrorAwareScoped<IRenewalsFeeValidator, RenewalsFeeValidator>("E006");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

			app.UseRewriter(new RewriteOptions().Add(RewriteRules.RewriteAlwaysOn));

			MigrateDatabase(app);
            SeedDatabase(app, env.ContentRootPath);

            Helper.UseIPOConfigurations(app, env);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        protected virtual void AddDatabase(IServiceCollection services)
        {
            var dbConnection = Configuration["FeeDbConnection"];

            services.AddIPOErrorAwareDbContext<IFeeDbContext, FeeDbContext>("E003", options =>
                options.UseSqlServer(dbConnection!));
        }

        protected virtual void AddHealthChecks(IServiceCollection services)
        {
            services.AddHealthChecks().AddTypeActivatedCheck<SQLHealthCheck>(
                $"Fee Service Database Health Check,",
                HealthStatus.Unhealthy,
                tags: new[] { HealthTags.Ready },
                args: new object[] { Configuration, "FeeDbConnection" }
                );
        }

        protected virtual void MigrateDatabase(IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<FeeDbContext>();
            dbContext!.Database.Migrate();
        }

        protected virtual void SeedDatabase(IApplicationBuilder app, string contentRootPath)
        {
            var scope = app.ApplicationServices.CreateScope();
            var dbRepository = scope.ServiceProvider.GetService<FeeDbRepository>();
            dbRepository!.SeedDatabase(dbRepository.GetSeedDataDirectory(contentRootPath));
        }

        protected virtual void AddSwaggerExamplesForType<T>(IServiceCollection services)
        {
            services.AddSwaggerExamplesFromAssemblyOf<T>();
        }

        private void AddDateFormatter(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                    options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
                });
        }
    }
}