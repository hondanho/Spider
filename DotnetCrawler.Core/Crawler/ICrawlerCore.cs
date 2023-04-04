
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class
    {
        CrawlerCore<T> AddRequest(SiteConfigDb request);
        CrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader);
        CrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler);
        Task Crawle(bool isReCrawleSmall = false);
    }
}
