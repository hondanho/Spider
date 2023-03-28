using DotnetCrawler.Data.Models.Novel;
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
        Task<PostDb> PostProcess(HtmlDocument document);
        Task<ChapDb> ChapProcess(HtmlDocument document);
    }
}
