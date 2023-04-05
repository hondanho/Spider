using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public class CrawlerCore<T> : ICrawlerCore<T> where T : class
    {
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;

        private readonly IRabitMQProducer _rabitMQProducer;
        private readonly string downloadPath = @"C:\DotnetCrawlercrawler\";

        public CrawlerCore(
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IRabitMQProducer rabitMQProducer,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IConfiguration configuration)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;
        }

        /// <summary>
        /// Save catgegory Data, add url category to category-queue
        /// </summary>
        /// <param name="isReCrawleSmall"></param>
        /// <returns></returns>
        public async Task Crawle(SiteConfigDb siteConfigDb, bool isReCrawleSmall = false)
        {
            SetDocumentDb(siteConfigDb.BasicSetting.Document);

            // add worker categoryDb
            if (siteConfigDb.CategorySetting.CategoryModels.Any())
            {
                var categoryDbs = _categoryDbRepository.FilterBy(cdb => siteConfigDb.CategorySetting.CategoryModels.Any(cgm => cgm.Slug == cdb.Slug));

                foreach (var category in siteConfigDb.CategorySetting.CategoryModels)
                { //fake
                    if (!string.IsNullOrEmpty(category?.Slug) && !string.IsNullOrEmpty(category?.Titlte) && !string.IsNullOrEmpty(category?.Url))
                    {
                        var categoryDb = categoryDbs.FirstOrDefault(cdb => cdb.Slug == category.Slug);
                        if (categoryDb == null)
                        {
                            categoryDb = new CategoryDb()
                            {
                                Slug = category.Slug,
                                Titlte = category.Titlte
                            };
                            await _categoryDbRepository.InsertOneAsync(categoryDb);
                            Console.WriteLine(string.Format("Category new: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Category existed: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                        }

                        _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCategoryName, new CategoryMessage()
                        {
                            CategoryUrl = category.Url,
                            CategoryIdString = categoryDb.Id,
                            SiteConfigDb = siteConfigDb
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Downloader url category, add next url category to category-queue, add url post to post-queue
        /// </summary>
        /// <param name="categoryMessage"></param>
        public async Task JobCategory(CategoryMessage categoryMessage)
        {
            var request = categoryMessage.SiteConfigDb;
            SetDocumentDb(request.BasicSetting.Document);

            var categoryUrl = categoryMessage.CategoryUrl;
            var categoryIdString = categoryMessage.CategoryIdString;
            var domain = categoryMessage.SiteConfigDb.BasicSetting.Domain;

            var htmlDocumentCategory = await DotnetCrawlerDownloader.Download(
                    categoryUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var postUrlModesl = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentCategory, request.CategorySetting.LinkPostSelector, domain);
            if (!postUrlModesl.Any())
            {
                return;
            }

            var postDbServers = _postDbRepository.FilterBy(pdb =>
                            pdb.CategoryId == categoryIdString &&
                            postUrlModesl.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                        );

            foreach (var postUrlModel in postUrlModesl)
            { //fake
                if (!string.IsNullOrEmpty(postUrlModel.Slug) && !string.IsNullOrEmpty(postUrlModel.Titlte) && !string.IsNullOrEmpty(postUrlModel.Url))
                {
                    var isDuplicatePost = IsDuplicatePost(request, postDbServers, postUrlModel);
                    _rabitMQProducer.SendMessage<PostMessage>(QueueName.QueuePostName, new PostMessage()
                    {
                        SiteConfigDb = request,
                        CategoryIdString = categoryIdString,
                        LinkPost = postUrlModel,
                        IsDuplicate = isDuplicatePost,
                        PostDb = postDbServers.FirstOrDefault(pdb =>
                                (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == postUrlModel.Slug : true) &&
                                (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == postUrlModel.Titlte : true)
                            )
                    });
                }
            }

            var urlCategoryNext = (await DotnetCrawlerPageLinkReader.GetLinks(htmlDocumentCategory, request.CategorySetting.PagingSelector, domain)).FirstOrDefault();
            if (string.IsNullOrEmpty(urlCategoryNext))
            {
                return;
            }
            _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCategoryName, new CategoryMessage()
            {
                CategoryUrl = urlCategoryNext,
                SiteConfigDb = request,
                CategoryIdString = categoryMessage.CategoryIdString
            });
        }

        /// <summary>
        /// Downloader post data and save, add url post to queue
        /// </summary>
        /// <param name="postMessage"></param>
        public async Task JobPost(PostMessage postMessage)
        {
            var request = postMessage.SiteConfigDb;
            SetDocumentDb(request.BasicSetting.Document);

            var categoryIdString = postMessage.CategoryIdString;
            var isReCrawleSmall = postMessage.IsReCrawleSmall;
            var linkPostModel = postMessage.LinkPost;
            var isDuplicate = postMessage.IsDuplicate;

            string idPostString = string.Empty;
            if (!request.PostSetting.IsHasChapter && isDuplicate)
            {
                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
                return;
            }

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    linkPostModel.Url,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var post = await (new CrawlerProcessor(request).PostProcess(categoryIdString, linkPostModel.Url, htmlDocumentPost));

            if (!isDuplicate)
            {
                await _postDbRepository.InsertOneAsync(post);
                Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}, Slug: {2}, Date: {3}", post.Id, post.Titlte, post.Slug, DateTime.Now));
            }
            else
            {
                post.Id = idPostString;
                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", post.Id, linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
            }

            // get info chap
            if (request.PostSetting.IsHasChapter)
            {
                _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueuePostDetailName, new PostDetailMessage()
                {
                    SiteConfigDb = request,
                    PostIdString = post.Id,
                    PostUrl = linkPostModel.Url
                });
            }
        }

        /// <summary>
        /// Download url post, add next url post to post-detail-queue, add url chap to queue
        /// </summary>
        /// <param name="postDetailMessage"></param>
        public async Task JobPostDetail(PostDetailMessage postDetailMessage)
        {
            var request = postDetailMessage.SiteConfigDb;
            SetDocumentDb(request.BasicSetting.Document);

            var postUrl = postDetailMessage.PostUrl;
            var postIdString = postDetailMessage.PostIdString;
            var domain = postDetailMessage.SiteConfigDb.BasicSetting.Domain;

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    postUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chapUrlModesl = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentPost, request.PostSetting.LinkChapSelector, domain);
            if (!chapUrlModesl.Any())
            {
                return;
            }

            var chapDbServers = _chapDbRepository.FilterBy(pdb =>
                        pdb.PostId == postIdString &&
                        chapUrlModesl.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                    );

            foreach (var chapUrlModel in chapUrlModesl)
            { //fake
                if (!string.IsNullOrEmpty(chapUrlModel.Slug) && !string.IsNullOrEmpty(chapUrlModel.Titlte) && !string.IsNullOrEmpty(chapUrlModel.Url))
                {
                    var isDuplicateChap = IsDuplicateChap(request, chapDbServers, chapUrlModel);
                    if (isDuplicateChap)
                    {
                        Console.WriteLine(string.Format("Chap existed: Title: {0}, Slug: {1}", chapUrlModel.Titlte, chapUrlModel.Slug));
                        continue;
                    }

                    _rabitMQProducer.SendMessage<ChapMessage>(QueueName.QueueChapName, new ChapMessage()
                    {
                        SiteConfigDb = request,
                        PostIdString = postIdString,
                        ChapUrl = chapUrlModel.Url
                    });
                }
            }

            var postUrlNext = (await DotnetCrawlerPageLinkReader.GetLinks(htmlDocumentPost, request.PostSetting.PagingSelector, domain)).FirstOrDefault();
            if (string.IsNullOrEmpty(postUrlNext))
            {
                return;
            }

            _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueuePostDetailName, new PostDetailMessage()
            {
                SiteConfigDb = request,
                PostIdString = postIdString,
                PostUrl = postUrlNext
            });
        }

        public async Task JobChap(ChapMessage chapMessage)
        {
            var request = chapMessage.SiteConfigDb;
            SetDocumentDb(request.BasicSetting.Document);

            var postIdString = chapMessage.PostIdString;
            var chapUrl = chapMessage.ChapUrl;
            var domain = chapMessage.SiteConfigDb.BasicSetting.Domain;

            var htmlDocumentChap = await DotnetCrawlerDownloader.Download(
                    chapUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chap = await (new CrawlerProcessor(request).ChapProcess(postIdString, chapUrl, htmlDocumentChap));
            await _chapDbRepository.InsertOneAsync(chap);
            Console.WriteLine(string.Format("Chap new: Title: {0}, Slug: {1}", chap.Titlte, chap.Slug));
        }

        private void SetDocumentDb(string documentName)
        {
            _postDbRepository.SetCollectionSave(documentName);
            _chapDbRepository.SetCollectionSave(documentName);
            _categoryDbRepository.SetCollectionSave(documentName);
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
