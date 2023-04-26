using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public interface ICrawlerService
    {
        Task<bool> CrawlerBySiteId(string siteId);
        Task ReCrawleAll();
        Task ReCrawleAllSchedule(int hour);
        Task UpdatePostChapAll();
        Task UpdatePostChapScheduleAll(int hour);
    }
}
