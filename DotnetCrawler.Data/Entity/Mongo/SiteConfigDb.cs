using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Setting;

namespace DotnetCrawler.Data.ModelDb
{
    [BsonCollection("siteconfig")]
    public class SiteConfigDb : Document
    {
        public BasicSetting BasicSetting { get; set; }
        public CategorySetting CategorySetting { get; set; }
        public ChapSetting ChapSetting { get; set; }
        public PostSetting PostSetting { get; set; }
        public SystemStatus SystemStatus { get; set; } 
    }
}
