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
using Microsoft.AspNetCore.Http.Features;
using DotnetCrawler.API.Service.Wordpress;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using System.Collections.Generic;
using System.Threading;
using System;
using WordPressPCL.Models;
using DotnetCrawler.API.RabitMQ;
using RabbitMQ.Client;
using DotnetCrawler.Data.Model;

namespace DotnetCrawler.Api
{
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services) {
            var mongoUrlBuilder = new MongoUrlBuilder(
                    string.Format(
                        "{0}/{1}",
                        Configuration.GetValue<string>("MongoDbSettings:ConnectionString"),
                        Configuration.GetValue<string>("MongoDbSettings:HangfireDb")
                    )
                );

            var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
            // Add Hangfire services. Hangfire.AspNetCore nuget required
            services.AddHangfire(configuration =>
                configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions {
                    MigrationOptions = new MongoMigrationOptions {
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    Prefix = "hangfire",
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                })
            );

            // Add the processing server as IHostedService
            services.AddHangfireServer(serverOptions => {
                serverOptions.ServerName = "Hangfire.Mongo server 1";
            });

            services.AddHangfireServer();
            services.AddHostedService<RabitMQConsumer>();
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crawler Site", Version = "v1" });
            });

            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDbSettings"));
            services.AddSingleton<IMongoDbSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            services.Configure<RabitMQSettings>(Configuration.GetSection("RabitMQSettings"));
            services.AddSingleton<IRabitMQSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<RabitMQSettings>>().Value);
            services.AddScoped<ICrawlerService, CrawlerService>();
            services.AddScoped<IWordpressSyncCore, WordpressSyncCore>();
            services.AddScoped<IRabitMQProducer, RabitMQProducer>();
            services.AddScoped<IWordpressService, WordpressService>();
            services.AddScoped(typeof(ICrawlerCore<>), typeof(CrawlerCore<>));
            services.Configure<FormOptions>(options => {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });
            services.Configure<BackgroundJobServerOptions>(options => {
                options.WorkerCount = 1;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCoreMongoDb v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHangfireDashboard("/worker");
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

        }
    }
}

