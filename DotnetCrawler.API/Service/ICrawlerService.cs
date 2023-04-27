using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public interface ICrawlerService
    {
        Task CrawleAllSchedule(int minute);
    }
}
