using DotnetCrawler.Base.Extension;
using DotnetCrawler.Core;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Repository;
using Hangfire;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<PostDb> _postDbRepository;

        public CrawlerService(
            IMongoRepository<CategoryDb> categoryDbRepository,
            IMongoRepository<PostDb> postDbRepository,
            ICrawlerCore<CategorySetting> dotnetCrawlerCore)
        {
            _postDbRepository = postDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _crawlerCore = dotnetCrawlerCore;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ReCrawleSchedule(int hour)
        {
            RecurringJob.AddOrUpdate(() => CheckingCrawle(), Cron.HourInterval(hour));
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ForceReCrawleSchedule() {
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList() ?? new List<CategoryDb>();
            foreach(var categoryDb in categoryDbs) {
                categoryDb.UrlCategoryPagingLatest = string.Empty;
                categoryDb.UrlCategoryPagingNext = categoryDb.Url;
                _categoryDbRepository.ReplaceOne(categoryDb);

                var postDbServers = _postDbRepository.FilterBy(pdb =>
                    pdb.CategorySlug == categoryDb.Slug
                ).ToList() ?? new List<PostDb>();
                foreach(var posdtDb in postDbServers) {
                    posdtDb.UrlPostPagingCrawleLatest = string.Empty;
                    _postDbRepository.ReplaceOne(posdtDb);
                }
            }

            Helper.Display("Update Now", Core.Extension.MessageType.HighSystemInfo);
            await CheckingCrawle();
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task Crawle()
        {
            await CheckingCrawle();
        }

        public async Task CheckingCrawle()
        {
            var isRunning = Helper.CheckJobExistRunning();
            if (isRunning)
            {
                Helper.Display("Crawle started", Core.Extension.MessageType.HighSystemInfo);
            }
            else
            {
                Helper.Display("Crawle starting", Core.Extension.MessageType.HighSystemInfo);
                await _crawlerCore.NextCategory();
            }
        }
    }
}
