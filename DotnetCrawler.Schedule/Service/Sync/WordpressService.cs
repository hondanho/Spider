using DotnetCrawler.Core;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using Hangfire;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service.Wordpress
{
    public class WordpressService : IWordpressService
    {
        private readonly IWordpressSyncCore _wordpressSyncCore;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;


        public WordpressService(
            IWordpressSyncCore wordpressSyncCore,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository
            )
        {
            _wordpressSyncCore = wordpressSyncCore;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        public async Task SyncDataBySite(string siteId) {
            var siteConfig = _siteConfigDbRepository.FindById(siteId);
            if (siteConfig != null) {
                BackgroundJob.Enqueue(() => _wordpressSyncCore.SyncDataBySite(siteConfig));
            }
        }

        public async Task SyncAllData()
        {
            var siteConfigs = _siteConfigDbRepository.AsQueryable().ToList();
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    BackgroundJob.Enqueue(() => _wordpressSyncCore.SyncDataBySite(siteConfig));
                }
            }
        }

        public async Task SyncDataSchedule(int hour) {
            RecurringJob.AddOrUpdate(() => SyncAllData(), Cron.HourInterval(hour));
        }
    }
}
