using DotnetCrawler.Base.Extension;
using DotnetCrawler.Core;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Repository;
using Hangfire;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Rabbit.Common.Display;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private int scheduleHourUpdatePostChap;

        public CrawlerService(
            IMongoRepository<CategoryDb> categoryDbRepository,
            IMongoRepository<PostDb> postDbRepository,
            IConfiguration configuration,
            ICrawlerCore<CategorySetting> dotnetCrawlerCore)
        {
            _postDbRepository = postDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _crawlerCore = dotnetCrawlerCore;
            scheduleHourUpdatePostChap = configuration.GetValue<int>("Setting:ScheduleHourUpdatePostChap") > 0 ?
                configuration.GetValue<int>("Setting:ScheduleHourUpdatePostChap") : 1;


        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task ReCrawleAllSchedule(int minute)
        {
            CheckingCrawle(true);
            RecurringJob.AddOrUpdate(() => CheckingCrawle(true), Cron.MinuteInterval(minute));
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task CrawleAllSchedule(int minute)
        {
            CheckingCrawle(false);
            RecurringJob.AddOrUpdate(() => CheckingCrawle(false), Cron.MinuteInterval(minute));
        }

        public async Task CheckingCrawle(bool isReCrawler)
        {
            var isRunning = Helper.CheckJobExistRunning();
            if (isRunning)
            {
                DisplayInfo<string>.For("Crawling").SetQueue("Crawling").Display(Color.Red);
            }
            else
            {
                DisplayInfo<string>.For("Crawle Now").SetQueue("Crawle Now").Display(Color.Red);

                // crawle
                var doCrawle = await _crawlerCore.Crawle(isReCrawler);

                // update post chap
                if (!doCrawle)
                {
                    await UpdateChapNow(isReCrawler);
                }
            }
        }

        private async Task UpdateChapNow(bool isReCrawler)
        {
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList() ?? new List<CategoryDb>();
            foreach (var categoryDb in categoryDbs)
            {
                categoryDb.UrlCategoryPagingLatest = string.Empty;
                categoryDb.UrlCategoryPagingNext = categoryDb.Url;
                _categoryDbRepository.ReplaceOne(categoryDb);

                if (isReCrawler)
                {
                    var postDbServers = _postDbRepository.FilterBy(pdb =>
                        pdb.CategorySlug == categoryDb.Slug
                    ).ToList() ?? new List<PostDb>();
                    foreach (var posdtDb in postDbServers)
                    {
                        posdtDb.UrlPostPagingCrawleLatest = string.Empty;
                        posdtDb.UrlPostPagingCrawleNext = posdtDb.Url;
                        _postDbRepository.ReplaceOne(posdtDb);
                    }
                }
            }

            DisplayInfo<string>.For("Update Now").SetQueue("Update Now").Display(Color.Red);
            await _crawlerCore.Crawle(isReCrawler);
        }
    }
}
