using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Api.Service;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Hangfire.Mongo.Migration.Strategies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using DotnetCrawler.Core;

namespace DotnetCrawler.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            var mongoUrlBuilder = new MongoUrlBuilder(
                    string.Format(
                        "{0}/{1}",
                        Configuration.GetValue<string>("MongoDbSettings:ConnectionString"),
                        Configuration.GetValue<string>("MongoDbSettings:HangfireDb")
                    )
                );
            var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
            // Add Hangfire services. Hangfire.AspNetCore nuget required
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    Prefix = "hangfire.mongo",
                    CheckConnection = true
                })
            );
            // Add the processing server as IHostedService
            services.AddHangfireServer(serverOptions =>
            {
                serverOptions.ServerName = "Hangfire.Mongo server 1";
            });

            services.AddHangfireServer();
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crawler Site", Version = "v1" });
            });

            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDbSettings"));
            services.AddSingleton<IMongoDbSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            services.AddScoped<ICrawlerService, CrawlerService>();
            services.AddScoped(typeof(IDotnetCrawlerCore<>), typeof(DotnetCrawlerCore<>));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCoreMongoDb v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHangfireDashboard("/worker");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}

