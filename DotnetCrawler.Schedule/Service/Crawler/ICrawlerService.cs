using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Setting;
using Hangfire;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
{
    public interface ICrawlerService
    {
        Task Crawler(SiteConfigDb crawlerRequest, StatusCrawler statusCrawler);
        Task Crawler(string siteId);
        Task CrawlerAll();
        Task ReCrawlerBig();
        Task ReCrawlerSmall();
        Task UpdateStatusSite(SiteConfigDb siteConfigDb, StatusCrawler statusCrawler);

        Task TaskD(int number, int time);
    }
}
