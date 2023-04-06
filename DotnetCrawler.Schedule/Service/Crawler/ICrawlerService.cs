using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Setting;
using Hangfire;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Service {
    public interface ICrawlerService {
        Task<bool> Crawler(string siteId);
        Task ReCrawleAll();
        Task UpdatePostChap();
    }
}
