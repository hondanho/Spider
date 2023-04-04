
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class
    {
        CrawlerCore<T> AddRequest(SiteConfigDb request);
        CrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader);
        CrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler);
        void JobPost(
            DotnetCrawlerPageLinkReader linkReader,
            HtmlDocument htmlDocumentCategory,
            CategoryDb category,
            bool isReCrawleSmall);
        Task Crawle(bool isReCrawleSmall = false);
    }
}
