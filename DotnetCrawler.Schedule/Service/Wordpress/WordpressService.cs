using DotnetCrawler.Core;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using Hangfire;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL;

namespace DotnetCrawler.API.Service.Wordpress
{
    public class WordpressService : IWordpressService
    {
        private readonly IWordpressSyncCore _wordpressSyncCore;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        private string WordpressUriApi { get; set; }
        private string WordpressUserName { get; set; }
        private string WordpressPassword { get; set; }

        public WordpressService(
            IWordpressSyncCore wordpressSyncCore,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            IConfiguration configuration
            )
        {
            _wordpressSyncCore = wordpressSyncCore;
            _siteConfigDbRepository = siteConfigDbRepository;
            WordpressUriApi = configuration.GetValue<string>("Setting:WordpressUriApi");
            WordpressUserName = configuration.GetValue<string>("Setting:WordpressUserName");
            WordpressPassword = configuration.GetValue<string>("Setting:WordpressPassword");
        }

        public async Task SyncAllData()
        {
            var siteConfigs = _siteConfigDbRepository.AsQueryable().ToList();
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    var syncData = _wordpressSyncCore
                        .AddRequest(siteConfig)
                        .AddWordpressClient(
                            new WordPressClient(siteConfig.BasicSetting.WordpressUriApi ?? WordpressUriApi) { },
                            siteConfig.BasicSetting.WordpressUserName ?? WordpressUserName,
                            siteConfig.BasicSetting.WordpressPassword ?? WordpressPassword 
                        );
                    await syncData.SyncAllData();
                }
            }
        }
    }
}
