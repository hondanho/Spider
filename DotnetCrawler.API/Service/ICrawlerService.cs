using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public interface ICrawlerService
    {
        Task CrawleAllSchedule(int minute);
        Task ReCrawleAllSchedule(int minute);
    }
}
