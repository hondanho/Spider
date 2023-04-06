using DotnetCrawler.Data.ModelDb;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service.Wordpress
{
    public interface IWordpressService
    {
        Task SyncAllData();
    }
}
