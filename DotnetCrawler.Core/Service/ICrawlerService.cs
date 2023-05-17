using System.Threading.Tasks;

namespace DotnetCrawler.API.Service
{
    public interface ICrawlerService
    {
        Task Crawle();
        Task ReCrawleSchedule(int hour);
        Task ForceReCrawleSchedule();
    }
}
