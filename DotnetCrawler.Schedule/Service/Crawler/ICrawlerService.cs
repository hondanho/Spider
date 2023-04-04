using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Setting;
using Hangfire;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
{
    public interface ICrawlerService
    {
        Task Crawler(string siteId);
        Task CrawlerAll();
        Task ReCrawlerBig();
        Task ReCrawlerSmall();
        Task TaskD(int number, int time);
    }
}
