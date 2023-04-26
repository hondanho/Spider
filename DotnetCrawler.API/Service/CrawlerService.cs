using DotnetCrawler.Core;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.Repository;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;
        private IRabitMQSettings _rabitMQSettings;

        public CrawlerService(
            ICrawlerCore<CategorySetting> dotnetCrawlerCore,
            IConfiguration configuration,
            IRabitMQSettings rabitMQSettings,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository)
        {
            _crawlerCore = dotnetCrawlerCore;
            _siteConfigDbRepository = siteConfigDbRepository;
            _rabitMQSettings = rabitMQSettings;
        }

        public async Task<bool> CrawlerBySiteId(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
                return false;
            var siteConfig = await _siteConfigDbRepository.FindByIdAsync(siteId);
            if (siteConfig == null)
                return false;

            // crawler
            var newSiteConfig = new SiteConfigDb()
            {
                BasicSetting = siteConfig.BasicSetting,
                CategorySetting = siteConfig.CategorySetting,
                PostSetting = siteConfig.PostSetting,
                ChapSetting = siteConfig.ChapSetting
            };
            BackgroundJob.Enqueue(() => Crawler(newSiteConfig));

            return true;
        }

        public async Task UpdatePostChapAll()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThapLai).ToList();
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    await _crawlerCore.Crawle(siteConfig, isUpdatePostChap: true);
                }
            }
        }

        public async Task UpdatePostChapScheduleAll(int hour)
        {
            RecurringJob.AddOrUpdate(() => ReCrawleAllCore(true), Cron.HourInterval(hour));
        }

        public async Task ReCrawleAll()
        {
            await ReCrawleAllCore(false);
        }

        public async Task ReCrawleAllSchedule(int hour)
        {
            RecurringJob.AddOrUpdate(() => ReCrawleAllCore(false), Cron.HourInterval(hour));
        }


        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task Crawler(SiteConfigDb siteConfig)
        {
            await _crawlerCore.Crawle(siteConfig);
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ReCrawleAllCore(bool isUpdatePostChap = false)
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThap).ToList();
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    await _crawlerCore.Crawle(siteConfig, isUpdatePostChap);
                }
            }
        }
    }
}
