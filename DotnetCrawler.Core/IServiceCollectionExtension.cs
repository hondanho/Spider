using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotnetCrawler.Core
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddMongoRepository(this IServiceCollection services)
        {
            services.AddSingleton<IMongoDbSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            return services;
        }
    }
}
