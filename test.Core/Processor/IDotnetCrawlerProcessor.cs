using DotnetCrawler.Data.Repository;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCrawler.Processor
{
    public interface IDotnetCrawlerProcessor<T> where T : class
    {
        Task<IEnumerable<T>> Process(HtmlDocument document);
    }
}
