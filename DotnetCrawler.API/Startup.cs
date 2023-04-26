using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using DotnetCrawler.Core;
using Microsoft.AspNetCore.Http.Features;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Core.RabitMQ;
using Hangfire.Console;
using Hangfire.MemoryStorage;
using System;
using Hangfire.Dashboard;
using DotnetCrawler.API.Service;

namespace DotnetCrawler.Api
{
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services) {
            // Add Hangfire services. Hangfire.AspNetCore nuget required
            services.AddHangfire(configuration =>
            {
                configuration.UseMemoryStorage();
                configuration.UseConsole();

            });

            services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
                options.WorkerCount = Math.Min(Environment.ProcessorCount * 5, Configuration.GetValue<int>("Setting:MaxWorkCount"));
            });
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 1
            });

            services.AddHostedService<RabitMQConsumer>();
            services.AddControllers();

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crawler Site", Version = "v1" });
            });

            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDbSettings"));
            services.AddSingleton<IMongoDbSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
            services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));

            services.Configure<RabitMQSettings>(Configuration.GetSection("RabitMQSettings"));
            services.AddSingleton<IRabitMQSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<RabitMQSettings>>().Value);
            services.AddSingleton<IRabitMQProducer, RabitMQProducer>();

            services.AddSingleton(typeof(ICrawlerCore<>), typeof(CrawlerCore<>));
            services.AddScoped<ICrawlerService, CrawlerService>();

            services.Configure<FormOptions>(options => {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCore 3.1 MongoDb v1"));
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHangfireDashboard("/worker", new DashboardOptions()
            {
                Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
            });
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

        }
    }

    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
