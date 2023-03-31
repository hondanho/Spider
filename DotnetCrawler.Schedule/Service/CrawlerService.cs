using DotnetCrawler.Core;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
{
    public class CrawlerService : ICrawlerService
    {
        private readonly IDotnetCrawlerCore<CategorySetting> _dotnetCrawlerCore;

        public CrawlerService(IDotnetCrawlerCore<CategorySetting> dotnetCrawlerCore)
        {
            _dotnetCrawlerCore = dotnetCrawlerCore;
        }

        public async Task Crawler(SiteConfigDb siteConfigDb)
        {
            var crawler = _dotnetCrawlerCore
                .AddRequest(siteConfigDb)
                .AddDownloader(new DotnetCrawlerDownloader
                {
                    DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                    DownloadPath = @"C:\DotnetCrawlercrawler\",
                    Proxys = siteConfigDb.BasicSetting.Proxys
                })
                .AddScheduler(new DotnetCrawlerScheduler() { });
            await crawler.Crawle();
        }

        public async Task ReCrawle(SiteConfigDb siteConfigDb)
        {
            var crawler = _dotnetCrawlerCore
                .AddRequest(siteConfigDb)
                .AddDownloader(new DotnetCrawlerDownloader
                {
                    DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                    DownloadPath = @"C:\DotnetCrawlercrawler\",
                    Proxys = siteConfigDb.BasicSetting.Proxys
                })
                .AddScheduler(new DotnetCrawlerScheduler() { });

            await crawler.ReCrawle();
        }
    }
}
