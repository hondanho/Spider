
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface IDotnetCrawlerCore<T> where T : class
    {
        Task Crawle();
    }
}
