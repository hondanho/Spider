
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface IDotnetCrawlerCore<T> where T : class
    {
        DotnetCrawlerCore<T> AddRequest(SiteConfigDb request);
        DotnetCrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader);
        DotnetCrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler);
        Task Crawle();
    }
}
