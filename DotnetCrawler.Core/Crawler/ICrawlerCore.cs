
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Entity;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class {
        Task<bool> Crawle(bool isUpdatePostChap = false);
        Task JobPost(PostMessage post);
        Task JobPostDetail(PostDetailMessage post);
    }
}
