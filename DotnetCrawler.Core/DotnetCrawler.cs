using DotnetCrawler.Data.AutoMap;
using DotnetCrawler.Data.Models.Novel;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Processor;
using DotnetCrawler.Request;
using DotnetCrawler.Scheduler;
using System;
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
            var linkReader = new DotnetCrawlerPageLinkReader(Request);
            var linkPostFromCategory = await linkReader.GetLinks(Request.CategorySetting.Url, Request.CategorySetting.LinkPostSelector, 0);

            foreach (var url in linkPostFromCategory)
            {
                var documentPost = await Downloader.Download(url);
                var postData = (new PostCrawlerProcessor(Request)).Process(documentPost);

                Console.WriteLine(postData.ToString());

                //await (new DotnetCrawlerPipeline<PostDb>()).Run(dataPostDb);
            }
        }
    }
}
