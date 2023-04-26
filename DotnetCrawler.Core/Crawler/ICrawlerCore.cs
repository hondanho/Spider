
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Entity;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class {
        Task Crawle(SiteConfigDb siteConfig, bool isUpdatePostChap = false);
        Task JobCategory(CategoryMessage categoryMessage);
        Task JobPost(PostMessage post);
        Task JobPostDetail(PostDetailMessage post);
        Task JobChap(ChapMessage chapMessage);
    }
}
