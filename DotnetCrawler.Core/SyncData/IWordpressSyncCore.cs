
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.ModelDb;
using System.Threading.Tasks;
using WordPressPCL;

namespace DotnetCrawler.Core
{
    public interface IWordpressSyncCore
    {
        Task SyncDataBySite(SiteConfigDb siteConfig);
        Task JobSyncPost(PostSyncMessage postSyncMessage);
        Task JobSyncChap(ChapSyncMessage chapSyncMessage);
    }
}
