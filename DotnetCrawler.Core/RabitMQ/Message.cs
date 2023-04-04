using DotnetCrawler.Data.Models;
using DotnetCrawler.Downloader;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Core.RabitMQ
{
    public class PostMessage
    {
        public DotnetCrawlerPageLinkReader LinkReader { get; set; }
        public HtmlDocument HtmlDocumentCategory { get; set; }
        public CategoryDb Category { get; set; }
        public bool IsReCrawleSmall { get; set; }
    }
}
