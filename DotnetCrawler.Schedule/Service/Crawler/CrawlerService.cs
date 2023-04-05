using DotnetCrawler.Core;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        public CrawlerService(ICrawlerCore<CategorySetting> dotnetCrawlerCore, IMongoRepository<SiteConfigDb> siteConfigDbRepository)
        {
            _crawlerCore = dotnetCrawlerCore;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        public async Task Crawler(string siteId)
        {
            if (string.IsNullOrEmpty(siteId)) return;
            var siteConfig = await _siteConfigDbRepository.FindByIdAsync(siteId);
            if (siteConfig == null) return;

            // crawler
            var newSiteConfig = new SiteConfigDb()
            {
                BasicSetting = siteConfig.BasicSetting,
                CategorySetting = siteConfig.CategorySetting,
                PostSetting = siteConfig.PostSetting,
                ChapSetting = siteConfig.ChapSetting
            };
            BackgroundJob.Enqueue(() => Crawler(newSiteConfig));
        }

        public async Task Crawler(SiteConfigDb siteConfig)
        {
            var crawler = _crawlerCore
                .AddRequest(siteConfig)
                .AddDownloader(new DotnetCrawlerDownloader
                {
                    DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                    DownloadPath = @"C:\DotnetCrawlercrawler\",
                    Proxys = siteConfig.BasicSetting.Proxys,
                    UserAgent = siteConfig.BasicSetting.UserAgent
                })
                .AddScheduler(new DotnetCrawlerScheduler() { });
            await crawler.Crawle();
        }

        public async Task ReCrawlerSmall()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThapLai).ToList();
            await CrawlerListSite(siteConfigs, isReCrawleSmall: true);
        }

        public async Task CrawlerAll()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThap).ToList();
            await CrawlerListSite(siteConfigs);
        }

        public async Task ReCrawlerBig()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThap).ToList();
            await CrawlerListSite(siteConfigs);
        }

        private async Task CrawlerListSite(
            List<SiteConfigDb> siteConfigs,
            bool isReCrawleSmall = false)
        {
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    // run recrawler
                    var crawler = _crawlerCore
                   .AddRequest(siteConfig)
                   .AddDownloader(new DotnetCrawlerDownloader
                   {
                       DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                       DownloadPath = @"C:\DotnetCrawlercrawler\",
                       Proxys = siteConfig.BasicSetting.Proxys,
                       UserAgent = siteConfig.BasicSetting.UserAgent
                   })
                   .AddScheduler(new DotnetCrawlerScheduler() { });
                    await crawler.Crawle(isReCrawleSmall);
                }
            }
        }

        public async Task TaskD(int number, int time)
        {
            Console.WriteLine($"Welcome Task {number} waitting {time}s");
            while (time > 0)
            {
                //Console.WriteLine($"Doing Task {number} time {time}");
                await Task.Delay(1000);
                time--;
            }
            Console.WriteLine($"done task {number}");
        }
    }
}
