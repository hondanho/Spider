
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Request;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface IDotnetCrawlerCore<T> where T : class
    {
        DotnetCrawlerCore<T> AddRequest(IDotnetCrawlerRequest request);
        DotnetCrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader);
        DotnetCrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler);
        Task Crawle();
    }
}
