
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Entity.Setting;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class {
        Task NextCategory(CategoryModel category = null);
        Task JobPost(PostMessage post);
        Task JobPostDetail(PostDetailMessage post);
    }
}
