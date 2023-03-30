using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Processor;
using DotnetCrawler.Request;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public class DotnetCrawlerCore<T> : IDotnetCrawlerCore<T> where T : class
    {
        public IDotnetCrawlerRequest Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;

        public DotnetCrawlerCore(IMongoRepository<PostDb> postDbRepository, IMongoRepository<ChapDb> chapDbRepository)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
        }

        public DotnetCrawlerCore<T> AddRequest(IDotnetCrawlerRequest request)
        {
            Request = request;
            return this;
        }

        public DotnetCrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader)
        {
            Downloader = downloader;
            return this;
        }

        public DotnetCrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler)
        {
            Scheduler = scheduler;
            return this;
        }

        public async Task Crawle() {
            // get list post
            var linkReader = new DotnetCrawlerPageLinkReader(Request);
            var htmlDocumentCategory = await Downloader.Download(Request.CategorySetting.Url);
            while (true)
            {
                var linksPost = await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.LinkPostSelector);
                if (!linksPost.Any())
                    break;

                foreach (var urlPost in linksPost.Take(2))
                { ///fake
                    if (string.IsNullOrEmpty(urlPost))
                        continue;

                    // get info post
                    var htmlDocumentPost = await Downloader.Download(urlPost);
                    var postData = await (new CrawlerProcessor(Request).PostProcess(htmlDocumentPost));
                    await _postDbRepository.InsertOneAsync(postData);
                    Console.WriteLine(string.Format("Post: Id: {0}, Title: {1}", postData.Id, postData.Titlte));

                    // get info chap
                    while (true)
                    {
                        var linksChap = await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.LinkChapSelector);
                        if (!linksChap.Any())
                            break;

                        foreach (var urlChap in linksChap.Take(2))
                        { ///fake
                            if (string.IsNullOrEmpty(urlChap))
                                continue;
                            var htmlDocumentChap = await Downloader.Download(urlChap);
                            var chapData = await (new CrawlerProcessor(Request).ChapProcess(htmlDocumentChap));
                            await _chapDbRepository.InsertOneAsync(chapData);
                            Console.WriteLine(string.Format("Chap: Id: {0}, Title: {1}", chapData.Id, chapData.Titlte));
                        }

                        var urlPostChapNext = (await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.PagingSelector)).FirstOrDefault();
                        if (string.IsNullOrEmpty(urlPostChapNext))
                            break;
                        htmlDocumentPost = await Downloader.Download(urlPostChapNext);
                        break; ///fake
                    }
                }

                var urlCategoryPostNext = (await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.PagingSelector)).FirstOrDefault();
                if (string.IsNullOrEmpty(urlCategoryPostNext))
                    break;
                htmlDocumentCategory = await Downloader.Download(urlCategoryPostNext);
            }
        }
    }
}
