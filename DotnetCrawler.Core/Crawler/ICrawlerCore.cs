
using DotnetCrawler.Core.RabitMQ;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class {
        Task<bool> Crawle(bool isReCrawler = false);
        Task JobPost(PostMessage post);
        Task JobPostDetail(PostDetailMessage post);
    }
}
