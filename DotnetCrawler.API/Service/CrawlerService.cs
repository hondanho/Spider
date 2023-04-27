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
            ICrawlerCore<CategorySetting> dotnetCrawlerCore)
        {
            _crawlerCore = dotnetCrawlerCore;
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
                var isCrawler = await _crawlerCore.Crawle(isUpdatePostChap: false);
                if(!isCrawler) {
                    await _crawlerCore.Crawle(isUpdatePostChap: true);
                }
            }
        }
    }
}
