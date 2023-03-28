using DotnetCrawler.Data.Models.Novel;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Processor;
using DotnetCrawler.Request;
using DotnetCrawler.Scheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public class DotnetCrawler<T> : IDotnetCrawler where T : class
    {
        public IDotnetCrawlerRequest Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }

        public DotnetCrawler()
        {

        }

        public DotnetCrawler<T> AddRequest(IDotnetCrawlerRequest request)
        {
            Request = request;
            return this;
        }

        public DotnetCrawler<T> AddDownloader(IDotnetCrawlerDownloader downloader)
        {
            Downloader = downloader;
            return this;
        }

        public DotnetCrawler<T> AddScheduler(IDotnetCrawlerScheduler scheduler)
        {
            Scheduler = scheduler;
            return this;
        }

        public async Task Crawle()
        {
            // get list post
            var linkReader = new DotnetCrawlerPageLinkReader(Request);
            var linksPost = await linkReader.GetLinks(Request.CategorySetting.Url, Request.CategorySetting.LinkPostSelector);

            foreach (var urlPost in linksPost)
            {
                // get info post
                var htmlDocumentPost = await Downloader.Download(urlPost);
                var postData = await (new CrawlerProcessor(Request).PostProcess(htmlDocumentPost));
                await (new DotnetCrawlerPipeline<PostDb>().Run(postData));
                Console.WriteLine(string.Format("Post: Id: {0}, Title: {1}", postData.Id, postData.Titlte));

                // get info chap
                while(true)
                {
                    var linksChap = await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.LinkChapSelector);
                    if (!linksChap.Any()) break;
                    foreach (var urlChap in linksChap.Take(2))
                    {
                        if (string.IsNullOrEmpty(urlChap)) continue;
                        var htmlDocumentChap = await Downloader.Download(urlChap);
                        var chapData = await (new CrawlerProcessor(Request).ChapProcess(htmlDocumentChap));
                        await (new DotnetCrawlerPipeline<ChapDb>().Run(chapData));
                        Console.WriteLine(string.Format("Chap: Id: {0}, Title: {1}", chapData.Id, chapData.Titlte));
                    }

                    var urlPostChapNext = (await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.PagingSelector)).FirstOrDefault();
                    if (string.IsNullOrEmpty(urlPostChapNext)) break;
                    htmlDocumentPost = await Downloader.Download(urlPostChapNext);
                }
            }
        }
    }
}
