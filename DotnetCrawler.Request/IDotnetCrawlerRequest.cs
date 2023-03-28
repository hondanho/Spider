using DotnetCrawler.Data.Setting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Request
{
    public interface IDotnetCrawlerRequest
    {
        BasicSetting BasicSetting { get; set; }
        CategorySetting CategorySetting { get; set; }
        ChapSetting ChapSetting { get; set; }
        PostSetting PostSetting { get; set; }
    }
}
