﻿using DotnetCrawler.Core;
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

            // remove all job running
            if (!string.IsNullOrEmpty(siteConfig.SystemStatus.JobId))
            {
                BackgroundJob.Delete(siteConfig.SystemStatus.JobId);
            }

            // crawler
            var newSiteConfig = new SiteConfigDb()
            {
                BasicSetting = siteConfig.BasicSetting,
                CategorySetting = siteConfig.CategorySetting,
                PostSetting = siteConfig.PostSetting,
                ChapSetting = siteConfig.ChapSetting
            };
            var jobId = BackgroundJob.Enqueue(() => Crawler(newSiteConfig, StatusCrawler.DATHUTHAP));
            siteConfig.SystemStatus.JobId = jobId;
            siteConfig.SystemStatus.Status = StatusCrawler.CRAWLER_DOING;
            await _siteConfigDbRepository.ReplaceOneAsync(siteConfig);
        }

        public async Task Crawler(SiteConfigDb siteConfig, StatusCrawler statusCrawler)
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

            // update status site
            await UpdateStatusSite(siteConfig, statusCrawler);
        }

        public async Task ReCrawlerSmall()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf =>
                scf.BasicSetting.IsThuThap &&
                scf.SystemStatus.Status != StatusCrawler.CRAWLER_DOING &&
                scf.SystemStatus.Status != StatusCrawler.RECRAWLER_BIG_DOING &&
                scf.SystemStatus.Status != StatusCrawler.RECRAWLER_SMALL_DOING
            ).ToList();
            await CrawlerListSite(siteConfigs, StatusCrawler.RECRAWLER_SMALL_DOING, isReCrawleSmall: true);
        }

        public async Task CrawlerAll()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf =>
                scf.BasicSetting.IsThuThap &&
                scf.SystemStatus.Status == StatusCrawler.DEFAULT
            ).ToList();
            await CrawlerListSite(siteConfigs, StatusCrawler.RECRAWLER_BIG_DOING);
        }

        public async Task ReCrawlerBig()
        {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf =>
                scf.BasicSetting.IsThuThap &&
                scf.SystemStatus.Status != StatusCrawler.CRAWLER_DOING &&
                scf.SystemStatus.Status != StatusCrawler.RECRAWLER_BIG_DOING
            ).ToList();
            await CrawlerListSite(siteConfigs, StatusCrawler.RECRAWLER_BIG_DOING);
        }

        private async Task CrawlerListSite(
            List<SiteConfigDb> siteConfigs,
            StatusCrawler statusCrawler,
            bool isReCrawleSmall = false)
        {
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    // remove all job running
                    if (!string.IsNullOrEmpty(siteConfig.SystemStatus.JobId))
                    {
                        BackgroundJob.Delete(siteConfig.SystemStatus.JobId);
                    }

                    // update status to doing
                    await UpdateStatusSite(siteConfig, statusCrawler);

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

                    // update status to done
                    await UpdateStatusSite(siteConfig, StatusCrawler.DATHUTHAP);
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

        public async Task UpdateStatusSite(SiteConfigDb siteConfig, StatusCrawler statusCrawler)
        {
            siteConfig.SystemStatus = new SystemStatus()
            {
                Status = statusCrawler,
                JobId = siteConfig.SystemStatus?.JobId
            };
            await _siteConfigDbRepository.ReplaceOneAsync(siteConfig);
        }
    }
}
