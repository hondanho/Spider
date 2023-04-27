using DotnetCrawler.Base.Extension;
using DotnetCrawler.Core;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Repository;
using Hangfire;
using MongoDB.Driver;
using Rabbit.Common.Display;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;

        public CrawlerService(
            ICrawlerCore<CategorySetting> dotnetCrawlerCore,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository)
        {
            _crawlerCore = dotnetCrawlerCore;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task CrawleAllSchedule(int minute)
        {
            CheckingCrawle();
            RecurringJob.AddOrUpdate(() => CheckingCrawle(), Cron.MinuteInterval(minute));
        }

        public async Task CheckingCrawle()
        {
            var isRunning = Helper.CheckJobExistRunning();
            if (isRunning)
            {
                DisplayInfo<string>.For("Crawling").SetQueue("Crawling").Display(Color.Red);
            }
            else
            {
                DisplayInfo<string>.For("Crawle Now").SetQueue("Crawle Now").Display(Color.Red);
                await _crawlerCore.Crawle(false);
            }
        }
    }
}
