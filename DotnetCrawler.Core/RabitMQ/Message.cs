using Amazon.Runtime.Internal;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordPressPCL.Models;

namespace DotnetCrawler.Core.RabitMQ {
    public class CategoryMessage {
        public string CategoryUrl { get; set; }
        public string CategoryIdString { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public BaseMessage BaseMessage { get; set; }
    }

    public class PostMessage {
        public LinkModel LinkPost { get; set; }
        public bool IsDuplicate { get; set; }
        public PostDb PostDb { get; set; }
        public string CategoryIdString { get; set; }
        public bool IsReCrawleSmall { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public BaseMessage BaseMessage { get; set; }
    }

    public class PostDetailMessage {
        public string PostIdString { get; set; }
        public string PostUrl { get; set; }
        public bool IsReCrawleSmall { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public BaseMessage BaseMessage { get; set; }
    }

    public class ChapMessage {
        public string PostIdString { get; set; }
        public string ChapUrl { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public BaseMessage BaseMessage { get; set; }
    }

    public class BaseMessage {
        public SiteConfigDb Request { get; set; }
        public IDotnetCrawlerDownloader Downloader { get; set; }
        public DotnetCrawlerPageLinkReader LinkReader { get; set; }

        public BaseMessage() { }
        public BaseMessage(SiteConfigDb siteConfigDb) {

            Downloader = new DotnetCrawlerDownloader {
                DownloderType = DotnetCrawlerDownloaderType.FromMemory,
                DownloadPath = @"C:\DotnetCrawlercrawler\",
                Proxys = siteConfigDb.BasicSetting.Proxys,
                UserAgent = siteConfigDb.BasicSetting.UserAgent
            };
            LinkReader = new DotnetCrawlerPageLinkReader();
            Request = siteConfigDb;
        }
    }
}
