using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Request;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service
{
    public interface ICrawlerService
    {
        Task Crawler(SiteConfigDb dotnetCrawlerRequest);
    }
}
