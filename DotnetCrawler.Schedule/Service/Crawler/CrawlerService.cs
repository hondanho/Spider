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

namespace DotnetCrawler.Api.Service {
    public class CrawlerService : ICrawlerService {
        private readonly ICrawlerCore<CategorySetting> _crawlerCore;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        public CrawlerService(ICrawlerCore<CategorySetting> dotnetCrawlerCore, IMongoRepository<SiteConfigDb> siteConfigDbRepository) {
            _crawlerCore = dotnetCrawlerCore;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        public async Task<bool> Crawler(string siteId) {
            if(string.IsNullOrEmpty(siteId))
                return false;
            var siteConfig = await _siteConfigDbRepository.FindByIdAsync(siteId);
            if(siteConfig == null)
                return false;

            // crawler
            var newSiteConfig = new SiteConfigDb() {
                BasicSetting = siteConfig.BasicSetting,
                CategorySetting = siteConfig.CategorySetting,
                PostSetting = siteConfig.PostSetting,
                ChapSetting = siteConfig.ChapSetting
            };
            BackgroundJob.Enqueue(() => Crawler(newSiteConfig));

            return true;
        }

        public async Task UpdatePostChap() {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThapLai).ToList();
            if(siteConfigs.Any()) {
                foreach(var siteConfig in siteConfigs) {
                    await _crawlerCore.Crawle(siteConfig, isUpdatePostChap: true);
                }
            }
        }

        public async Task ReCrawleAll() {
            var siteConfigs = _siteConfigDbRepository.FilterBy(scf => scf.BasicSetting.IsThuThap).ToList();
            if(siteConfigs.Any()) {
                foreach(var siteConfig in siteConfigs) {
                    await _crawlerCore.Crawle(siteConfig);
                }
            }
        }

        private async Task Crawler(SiteConfigDb siteConfig) {
            await _crawlerCore.Crawle(siteConfig);
        }
    }
}
