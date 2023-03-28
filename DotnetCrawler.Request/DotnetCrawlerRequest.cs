using DotnetCrawler.Data.Setting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Request
{
    public class DotnetCrawlerRequest : IDotnetCrawlerRequest
    {
        public BasicSetting BasicSetting { get; set; }
        public CategorySetting CategorySetting { get; set; }
        public ChapSetting ChapSetting { get; set; }
        public PostSetting PostSetting { get; set; }

        public DotnetCrawlerRequest(BasicSetting basicSetting, CategorySetting categorySetting, ChapSetting chapSetting, PostSetting postSetting) {
            BasicSetting = basicSetting;
            CategorySetting = categorySetting;
            ChapSetting = chapSetting;
            PostSetting = postSetting;
        }
    }
}
