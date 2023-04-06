
using DotnetCrawler.Data.ModelDb;
using System.Collections.Generic;
using System.Threading.Tasks;
using WordPressPCL;

namespace DotnetCrawler.Core
{
    public interface IWordpressSyncCore
    {
        WordpressSyncCore AddRequest(SiteConfigDb request);
        WordpressSyncCore AddWordpressClient(WordPressClient wordPressClient, string username, string password);
        Task SyncAllData();
    }
}
