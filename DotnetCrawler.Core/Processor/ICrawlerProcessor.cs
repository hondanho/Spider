using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCrawler.Processor
{
    public interface ICrawlerProcessor
    {
        Task<PostDb> PostProcess(string categorySlug, string url, HtmlDocument document);
        Task<ChapDb> ChapProcess(string postSlug, string url, HtmlDocument document);
    }
}
