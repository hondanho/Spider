using DotnetCrawler.Core;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
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

        public async Task ClearAllJobAndQueue()
        {
            using (var connection = JobStorage.Current.GetConnection())
            {
                foreach (var recurringJob in connection.GetRecurringJobs())
                {
                    RecurringJob.RemoveIfExists(recurringJob.Id);
                }
            }

            // clear queue
            var factory = new ConnectionFactory
            {
                HostName = _rabitMQSettings.HostName,
                UserName = _rabitMQSettings.UserName,
                Password = _rabitMQSettings.Password,
            };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {

                    var queues = new QueueName();
                    FieldInfo[] fields = queues.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    var constantFields = fields.Where(f => f.IsLiteral && !f.IsInitOnly);

                    foreach (var prop in constantFields)
                    {
                        string propValue = prop.GetValue(queues)?.ToString();

                        if (!string.IsNullOrEmpty(propValue))
                        {
                            channel.QueueDelete(propValue);
                        }
                    }
                }
            }
        }

        public async Task Crawler(SiteConfigDb siteConfig)
        {
            await _crawlerCore.Crawle(siteConfig);
        }

        private async Task ReCrawleAllCore(bool isUpdatePostChap = false)
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
