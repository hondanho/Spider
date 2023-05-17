
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Entity.Setting;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class {
        Task NextCategory(CategoryModel category = null);
        Task JobPostData(PostMessage post);
    }
}
