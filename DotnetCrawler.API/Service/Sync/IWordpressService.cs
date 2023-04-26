using DotnetCrawler.Data.ModelDb;
using System.Threading.Tasks;

namespace DotnetCrawler.API.Service.Wordpress
{
    public interface IWordpressService
    {
        Task SyncAllData();
        Task SyncDataSchedule(int hour);
        Task SyncDataBySite(string siteId);
    }
}
