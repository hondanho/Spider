using Amazon.Runtime.Internal;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public class CrawlerCore<T> : ICrawlerCore<T> where T : class
    {
        public SiteConfigDb Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        private int crawlerTaskCount { get; set; }

        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;

        public CrawlerCore(
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IConfiguration configuration)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            crawlerTaskCount = configuration.GetValue<int>("Setting:CrawlerTaskCount");
        }

        public CrawlerCore<T> AddRequest(SiteConfigDb request)
        {
            Request = request;
            return this;
        }

        public CrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader)
        {
            Downloader = downloader;
            return this;
        }

        public CrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler)
        {
            Scheduler = scheduler;
            return this;
        }

        public async Task Crawle(bool isReCrawleSmall = false)
        {
            // set collection save
            _postDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            var linkReader = new DotnetCrawlerPageLinkReader(Request);

            // get data category
            var category = _categoryDbRepository.FindOne(cdb => cdb.Slug == Request.CategorySetting.Slug);
            if (category == null)
            {
                category = new CategoryDb()
                {
                    Slug = Request.CategorySetting.Slug,
                    Titlte = Request.CategorySetting.Titlte
                };
                await _categoryDbRepository.InsertOneAsync(category);
                Console.WriteLine(string.Format("Category new: Slug: {0}, Title: {1}", category.Slug, category.Titlte));
            }
            else
            {
                Console.WriteLine(string.Format("Category existed: Slug: {0}, Title: {1}", category.Slug, category.Titlte));
            }

            var urlCategoryCrawleFirst =
                            isReCrawleSmall &&
                            category != null &&
                            !string.IsNullOrEmpty(category.UrlListPostPagingLatest)
                    ? category.UrlListPostPagingLatest : Request.CategorySetting.Url;
            var htmlDocumentCategory = await Downloader.Download(urlCategoryCrawleFirst);

            // get list post
            GetPost(linkReader, htmlDocumentCategory, category, isReCrawleSmall);
            Console.WriteLine(string.Format("Crawler Done {0}", DateTime.Now));
        }

        private async void GetPost(
            DotnetCrawlerPageLinkReader linkReader,
            HtmlDocument htmlDocumentCategory,
            CategoryDb category,
            bool isReCrawleSmall)
        {
            while (true)
            {
                var linksPost = await linkReader.GetPageLinkModel(htmlDocumentCategory, Request.CategorySetting.LinkPostSelector);
                if (!linksPost.Any())
                    break;

                var postServers = _postDbRepository.FilterBy(pdb =>
                                        pdb.CategoryId == category.IdString &&
                                        linksPost.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                                    );

                Queue<LinkModel> jobQueue = new Queue<LinkModel>(linksPost);
                int jobCount = jobQueue.Count(); // Số lượng công việc cần thực hiện
                int counter = Math.Min(crawlerTaskCount, jobCount); // Đặt giá trị ban đầu cho counter
                Task[] tasks = new Task[counter];
                for (int i = 0; i < counter; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        while (jobQueue.TryDequeue(out var linkPost))
                        {
                            // Thực hiện công việc

                            // check post duplicate
                            string idPostString = string.Empty;
                            var isDuplicate = IsDuplicatePost(Request, postServers, linkPost);
                            if (!Request.PostSetting.IsHasChapter && isDuplicate)
                            {
                                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", linkPost.Titlte, linkPost.Slug, DateTime.Now));
                                continue;
                            }

                            var postExist = postServers.FirstOrDefault(pdb =>
                                            (Request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == linkPost.Slug : true) &&
                                            (Request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == linkPost.Titlte : true)
                                        );
                            var urlPostCrawleFirst =
                           isReCrawleSmall &&
                           !string.IsNullOrEmpty(postExist?.UrlListChapPagingLatest)
                   ? postExist.UrlListChapPagingLatest : linkPost.Url;

                            var htmlDocumentPost = await Downloader.Download(urlPostCrawleFirst);
                            var post = await (new CrawlerProcessor(Request).PostProcess(category.IdString, linkPost.Url, htmlDocumentPost));

                            if (!isDuplicate)
                            {
                                await _postDbRepository.InsertOneAsync(post);
                                Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}, Slug: {2}, Date: {3}", post.IdString, post.Titlte, post.Slug, DateTime.Now));
                            }
                            else
                            {
                                post.IdString = idPostString;
                                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", post.IdString, linkPost.Titlte, linkPost.Slug, DateTime.Now));
                            }

                            // get info chap
                            if (Request.PostSetting.IsHasChapter)
                            {
                                GetChap(linkReader, htmlDocumentPost, post);
                            }
                            await Task.Delay(1000);
                        }
                    });
                }
                Task.WaitAll(tasks);

                var urlCategoryPostNext = (await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.PagingSelector)).FirstOrDefault();
                if (string.IsNullOrEmpty(urlCategoryPostNext))
                    break;
                htmlDocumentCategory = await Downloader.Download(urlCategoryPostNext);
                category.UrlListPostPagingLatest = urlCategoryPostNext;
                await _categoryDbRepository.ReplaceOneAsync(category);
            }
        }

        private async void GetChap(DotnetCrawlerPageLinkReader linkReader, HtmlDocument htmlDocumentPost, PostDb post)
        {
            while (true)
            {
                var linksChap = await linkReader.GetPageLinkModel(htmlDocumentPost, Request.PostSetting.LinkChapSelector);
                if (!linksChap.Any())
                    break;

                var chapServer = _chapDbRepository.FilterBy(pdb =>
                                    pdb.PostId == post.IdString &&
                                    linksChap.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                                );

                foreach (var linkChap in linksChap)
                {
                    // check chap duplicate
                    var isDuplicateChap = IsDuplicateChap(Request, chapServer, linkChap);
                    if (isDuplicateChap)
                    {
                        Console.WriteLine(string.Format("Chap existed: Title: {0}, Slug: {1}", linkChap.Titlte, linkChap.Slug));
                    }
                    else
                    {
                        var htmlDocumentChap = await Downloader.Download(linkChap.Url);
                        var chap = await (new CrawlerProcessor(Request).ChapProcess(post.IdString, linkChap.Url, htmlDocumentChap));
                        await _chapDbRepository.InsertOneAsync(chap);
                        Console.WriteLine(string.Format("Chap new: Title: {0}, Slug: {1}", chap.Titlte, chap.Slug));
                    }
                }

                var urlPostChapNext = (await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.PagingSelector)).FirstOrDefault();
                if (string.IsNullOrEmpty(urlPostChapNext))
                    break;
                htmlDocumentPost = await Downloader.Download(urlPostChapNext);
                post.UrlListChapPagingLatest = urlPostChapNext;
                await _postDbRepository.ReplaceOneAsync(post);
            }
        }

        private bool IsDuplicatePost(SiteConfigDb request, IEnumerable<PostDb> postServers, LinkModel linkPost)
        {
            if (request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost || postServers.Any())
            {
                var postExist = postServers.FirstOrDefault(pdb =>
                                            (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == linkPost.Slug : true) &&
                                            (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == linkPost.Titlte : true)
                                        );
                return postExist != null;
            }

            return false;
        }

        private bool IsDuplicateChap(SiteConfigDb request, IEnumerable<ChapDb> chapServers, LinkModel linkChap)
        {
            if (request.BasicSetting.CheckDuplicateSlugChap || request.BasicSetting.CheckDuplicateTitleChap || chapServers.Any())
            {
                var chapExist = chapServers.FirstOrDefault(pdb =>
                                            (request.BasicSetting.CheckDuplicateSlugChap ? pdb.Slug == linkChap.Slug : true) &&
                                            (request.BasicSetting.CheckDuplicateTitleChap ? pdb.Titlte == linkChap.Titlte : true)
                                        );
                return chapExist != null;
            }

            return false;
        }
    }
}
