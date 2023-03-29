using DotnetCrawler.Core;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Processor;
using DotnetCrawler.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetCrawler.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var crawler = new DotnetCrawler<CategorySetting>()
                                 .AddRequest(new DotnetCrawlerRequest(
                                     new BasicSetting() {
                                         CheckDuplicateUrlPost = true,
                                         CheckDuplicateTitlePost = true,
                                         CheckDuplicateTitleChapter = true,
                                         CheckDuplicateUrlChapter = true,
                                         IsThuThap = true,
                                         IsThuThapLai = true
                                     },
                                     new CategorySetting() {
                                         Domain = "https://novelfull.com",
                                         Url = "https://novelfull.com/genre/Shounen",
                                         LinkPostSelector = @"#list-page .truyen-title a",
                                         PagingSelector = @".pagination li.active + li a",
                                         TimeOut = 5000
                                     },
                                     new PostSetting() {
                                         Titlte = "#truyen .col-info-desc .desc:first-child .title",
                                         Description = "#truyen .desc-text",
                                         Metadata = new Dictionary<string, string>() {
                                            { "tw_status", ".col-truyen-main .info div:last-child a"}
                                        },
                                         Taxonomies = new Dictionary<string, string>() {
                                            { "tac-gia", ".col-truyen-main .info-holder div:first-child > a" },
                                            { "category", ".col-truyen-main .info div:nth-child(3) a" }
                                        },
                                         Avatar = "#truyen .col-truyen-main .book  img",
                                         RemoveNodeElement = new List<string>() { "link", "script", "video", "iframe", "style" },
                                         RemoveElementCssSelector = new List<string>() { },
                                         IsHasChapter = true,
                                         PagingSelector = ".pagination li.active + li a",
                                         LinkChapSelector = "#list-chapter .list-chapter li a"
                                     },
                                     new ChapSetting() {
                                         Titlte = "#chapter .chapter-title .chapter-text",
                                         Content = "#chapter-content",
                                         Slug = ".chapter-text",
                                         RemoveElementCssSelector = new List<string>() { },
                                         RemoveElement = new List<string>() { "link", "script", "video", "iframe", "style" }
                                     }
                                    )
                                 )
                                 .AddDownloader(new DotnetCrawlerDownloader
                                 {
                                     DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                                     DownloadPath = @"C:\DotnetCrawlercrawler\",
                                     Proxys = new List<string>() {
                                         "2.56.119.93:5074:tjrustix:ixjv3exklx7y",
                                         "185.199.229.156:7492:tjrustix:ixjv3exklx7y",
                                         "185.199.228.220:7300:tjrustix:ixjv3exklx7y",
                                         "185.199.231.45:8382:tjrustix:ixjv3exklx7y",
                                         "188.74.210.207:8382:tjrustix:ixjv3exklx7y"
                                     }
                                 });

            await crawler.Crawle();
        }
    }
}
