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

namespace DotnetCrawler.Sample {
    class Program {
        static void Main(string[] args) {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args) {
            var crawler = new DotnetCrawler<CategorySetting>()
                                 .AddRequest(new DotnetCrawlerRequest(
                                     new BasicSetting(),
                                     new CategorySetting() {
                                         Domain = "https://novelfull.com",
                                         Url = "https://novelfull.com/genre/Shounen",
                                         LinkPostSelector = @"#list-page .truyen-title a",
                                         PagingSelector = @"#list-page .truyen-title a",
                                         TimeOut = 5000
                                     },
                                     new ChapSetting() {
                                         Titlte = "#chapter .chapter-title .chapter-text",
                                         Content = "#chapter-content",
                                         Slug = ".chapter-text",
                                         RemoveElement = new List<string>() { "link", "script", "video", "iframe", "style" }
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
                                          RemoveElement = new List<string>() { "link", "script", "video", "iframe", "style" },
                                          StringReplace = new Dictionary<string, string>() {
                                            { "novelfull.com", "pdfreader34.site" }
                                        },
                                          IsHasChapter = true,
                                          PagingSelector = ".pagination li.active + li a",
                                          LinkChapSelector = "#list-chapter .list-chapter li a"
                                      }
                                    )
                                 )
                                 .AddDownloader(new DotnetCrawlerDownloader {
                                     DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                                     DownloadPath = @"C:\DotnetCrawlercrawler\"
                                 });

            await crawler.Crawle();
        }
    }
}
