using DotnetCrawler.Base.Extension;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostStatus = DotnetCrawler.Data.Constants.PostStatus;

namespace DotnetCrawler.Core
{
    public class CrawlerCore<T> : ICrawlerCore<T> where T : class
    {
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        private readonly IRabitMQProducer _rabitMQProducer;
        private readonly string downloadPath = @"C:\DotnetCrawlercrawler\";
        private string databaseName { get; set; }

        public CrawlerCore(
            IRabitMQProducer rabitMQProducer,
            IConfiguration configuration,
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            IMongoRepository<CategoryDb> categoryDbRepository)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;
            _siteConfigDbRepository = siteConfigDbRepository;

            databaseName = configuration.GetValue<string>("MongoDbSettings:DatabaseName");
        }

        /// <summary>
        /// Save catgegory Data, add url category to category-queue
        /// </summary>
        public async Task<bool> Crawle(bool isReCrawler = false)
        {
            var isCrarwler = false;
            var siteConfigs = _siteConfigDbRepository.AsQueryable().ToList();
            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    SetDocumentDb(databaseName);
                    var categoryModels = siteConfig.CategorySetting.CategoryModels;

                    // add worker categoryDb
                    if (categoryModels.Any())
                    {
                        var categoryDbs = _categoryDbRepository.FilterBy(cdb => categoryModels.Any(cgm => cgm.Slug == cdb.Slug)).ToList();

                        foreach (var category in categoryModels)
                        {
                            if (!string.IsNullOrEmpty(category?.Slug) && !string.IsNullOrEmpty(category?.Titlte) && !string.IsNullOrEmpty(category?.Url))
                            {
                                var slugCategory = Helper.CleanSlug(category.Slug);
                                var categoryDb = categoryDbs.FirstOrDefault(cdb => cdb.Slug == slugCategory);

                                // insert category
                                if (categoryDb == null)
                                {
                                    categoryDb = new CategoryDb()
                                    {
                                        Slug = slugCategory,
                                        Titlte = category.Titlte,
                                        Url = category.Url
                                    };
                                    await _categoryDbRepository.InsertOneAsync(categoryDb);
                                    Console.WriteLine(string.Format("CATEGORY NEW: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));
                                    
                                    await JobCategory(siteConfig, category.Url, categoryDb, isReCrawler);
                                    isCrarwler = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("CATEGORY EXIST: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                                    
                                    if (!string.IsNullOrEmpty(categoryDb.UrlCategoryPagingNext))
                                    {
                                        await JobCategory(siteConfig, categoryDb.UrlCategoryPagingNext, categoryDb, isReCrawler);
                                        isCrarwler = true;
                                        break;
                                    }

                                    if (string.IsNullOrEmpty(categoryDb.UrlCategoryPagingNext) &&
                                    !string.IsNullOrEmpty(categoryDb.UrlCategoryPagingLatest))
                                    {
                                        var isExistNewPost = await CheckExistNewPost(siteConfig, categoryDb.UrlCategoryPagingLatest, categoryDb);
                                        if (isExistNewPost)
                                        {
                                            await JobCategory(siteConfig, categoryDb.UrlCategoryPagingLatest, categoryDb, isReCrawler);
                                            isCrarwler = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return isCrarwler;
        }

        /// <summary>
        /// Downloader url category, add next url category to category-queue, add url post to post-queue
        /// </summary>
        /// <param name="categoryMessage"></param>
        public async Task JobCategory(SiteConfigDb request, string categoryUrl, CategoryDb categoryDb, bool isReCrawler)
        {
            var htmlDocumentCategory = await DotnetCrawlerDownloader.Download(
                    categoryUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );

            var postUrlModels = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentCategory, request.CategorySetting.LinkPostSelector, request.BasicSetting.Domain);
            if (!postUrlModels.Any())
            {
                return;
            }
            var pageCategoryNumber = 1;
            if (!string.IsNullOrEmpty(categoryUrl))
            {
                pageCategoryNumber = Helper.GetPageNumberFromRegex(categoryUrl, request.CategorySetting.PagingNumberRegex);
            }
            var postDbServers = _postDbRepository.FilterBy(pdb =>
                            postUrlModels.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                        ).ToList() ?? new List<PostDb>();
            for (int i = 0; i < postUrlModels.Count(); i++)
            {
                var postUrlModel = postUrlModels.ToList()[i];
                if (!string.IsNullOrEmpty(postUrlModel.Slug) && !string.IsNullOrEmpty(postUrlModel.Titlte) && !string.IsNullOrEmpty(postUrlModel.Url))
                {
                    var isDuplicatePost = IsDuplicatePost(request, postDbServers, postUrlModel);
                    var postDb = postDbServers.FirstOrDefault(pdb =>
                                (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == postUrlModel.Slug : true) &&
                                (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == postUrlModel.Titlte : true)
                            );

                    if (postDb?.Status == PostStatus.Completed) {
                        Console.Write(true);
                    }
                    // check completed then continue
                    if (postDb?.Status == PostStatus.Completed && !isReCrawler)
                    {
                        continue;
                    }

                    // check la update post chap
                    if (!string.IsNullOrEmpty(postDb?.UrlPostPagingCrawleNext) && request.PostSetting.IsHasChapter)
                    {
                        postUrlModel.Url = postDb.UrlPostPagingCrawleNext;
                    }

                    var index = (pageCategoryNumber - 1) * request.CategorySetting.AmountPostInCategory + (i + 1);
                    _rabitMQProducer.SendMessage<PostMessage>(QueueName.QueueCrawlePost, new PostMessage()
                    {
                        SiteConfigDb = request,
                        CategorySlug = categoryDb.Slug,
                        LinkPostCrawle = postUrlModel,
                        IsDuplicate = isDuplicatePost,
                        Index = index
                    });
                }
            }

            // save next url process
            var urlCategoryNext = (
                    await DotnetCrawlerPageLinkReader.GetLinks(
                    htmlDocumentCategory,
                    request.CategorySetting.PagingSelector,
                    request.BasicSetting.Domain)
                ).FirstOrDefault();
            categoryDb.UrlCategoryPagingNext = urlCategoryNext;
            categoryDb.UrlCategoryPagingLatest = categoryUrl;
            await _categoryDbRepository.ReplaceOneAsync(categoryDb);
        }

        /// <summary>
        /// Downloader post data and save, add url post to queue
        /// </summary>
        /// <param name="postMessage"></param>
        public async Task JobPost(PostMessage postMessage)
        {
            var request = postMessage.SiteConfigDb;
            var categorySlug = postMessage.CategorySlug;
            var linkPostCrawle = postMessage.LinkPostCrawle;
            var isDuplicate = postMessage.IsDuplicate;
            var index = postMessage.Index;
            SetDocumentDb(databaseName);

            if (!request.PostSetting.IsHasChapter && isDuplicate)
            {
                Console.WriteLine(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostCrawle.Titlte, linkPostCrawle.Slug, DateTime.Now));
                return;
            }

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    linkPostCrawle.Url,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var post = await CrawlerProcessor.PostProcess(request, categorySlug, linkPostCrawle.Url, index, htmlDocumentPost, databaseName);

            if (!isDuplicate)
            {
                await _postDbRepository.InsertOneAsync(post);
                Console.WriteLine(string.Format("POST NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}, Date: {4}", post.Id, post.Index, post.Titlte, post.Slug, DateTime.Now));
            }
            else
            {
                Console.WriteLine(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostCrawle.Titlte, linkPostCrawle.Slug, DateTime.Now));
            }

            // get info chap
            if (request.PostSetting.IsHasChapter)
            {
                _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueueCrawlePostDetail, new PostDetailMessage()
                {
                    SiteConfigDb = request,
                    PostDb = post,
                    UrlPostCrawleNext = linkPostCrawle.Url
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
            SetDocumentDb(databaseName);

            var postUrl = postDetailMessage.UrlPostCrawleNext;
            var postSlug = postDetailMessage.PostDb.Slug;
            var domain = postDetailMessage.SiteConfigDb.BasicSetting.Domain;

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    postUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chapUrlModels = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentPost, request.PostSetting.LinkChapSelector, domain);
            if (!chapUrlModels.Any())
            {
                return;
            }

            var pagePostNumber = 1;
            if (!string.IsNullOrEmpty(postUrl))
            {
                pagePostNumber = Helper.GetPageNumberFromRegex(postUrl, request.PostSetting.PagingNumberRegex);
            }

            var chapDbServers = _chapDbRepository.FilterBy(pdb =>
                    chapUrlModels.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                ).ToList();

            for (int i = 0; i < chapUrlModels.Count(); i++)
            {
                var chapUrlModel = chapUrlModels.ToList()[i];
                if (!string.IsNullOrEmpty(chapUrlModel.Slug) && !string.IsNullOrEmpty(chapUrlModel.Titlte) && !string.IsNullOrEmpty(chapUrlModel.Url))
                {
                    var isDuplicateChap = IsDuplicateChap(request, chapDbServers, chapUrlModel);
                    if (isDuplicateChap)
                    {
                        Console.WriteLine(string.Format("CHAP EXIST: Title: {0}, Slug: {1}", chapUrlModel.Titlte, chapUrlModel.Slug));
                        continue;
                    }

                    var index = (pagePostNumber - 1) * request.PostSetting.AmountChapInPost + (i + 1);
                    JobChap(request, postSlug, chapUrlModel.Url, index);
                }
            }

            var postUrlNext = (await DotnetCrawlerPageLinkReader.GetLinks(htmlDocumentPost, request.PostSetting.PagingSelector, domain)).FirstOrDefault();
            if (string.IsNullOrEmpty(postUrlNext))
            {
                return;
            }

            _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueueCrawlePostDetail, new PostDetailMessage()
            {
                SiteConfigDb = request,
                PostDb = postDetailMessage.PostDb,
                UrlPostCrawleNext = postUrlNext
            });
            postDetailMessage.PostDb.UrlPostPagingCrawleNext = postUrlNext;
            postDetailMessage.PostDb.UrlPostPagingCrawleLatest = postUrl;
            await _postDbRepository.ReplaceOneAsync(postDetailMessage.PostDb);
        }

        public async Task JobChap(SiteConfigDb siteConfigDb, string slugPost, string urlChap, int index)
        {
            SetDocumentDb(databaseName);

            var htmlDocumentChap = await DotnetCrawlerDownloader.Download(
                    urlChap,
                    downloadPath,
                    siteConfigDb.BasicSetting.Proxys,
                    siteConfigDb.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chap = await CrawlerProcessor.ChapProcess(siteConfigDb, slugPost, urlChap, index, htmlDocumentChap);
            await _chapDbRepository.InsertOneAsync(chap);
            Console.WriteLine(string.Format("CHAP NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}", chap.Id, chap.Index, chap.Titlte, chap.Slug));
        }

        private void SetDocumentDb(string documentName)
        {
            _postDbRepository.SetCollectionSave(documentName);
            _chapDbRepository.SetCollectionSave(documentName);
            _categoryDbRepository.SetCollectionSave(documentName);
        }

        private async Task<bool> CheckExistNewPost(SiteConfigDb request, string urlCategory, CategoryDb categoryDb)
        {
            var result = false;

            if (!string.IsNullOrEmpty(urlCategory))
            {
                var htmlDocumentCategory = await DotnetCrawlerDownloader.Download(
                    urlCategory,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
                var postUrlModels = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentCategory, request.CategorySetting.LinkPostSelector, request.BasicSetting.Domain);
                if (!postUrlModels.Any())
                {
                    return false;
                }

                var postDbServers = _postDbRepository.FilterBy(pdb =>
                            pdb.CategorySlug == categoryDb.Slug &&
                            postUrlModels.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                        ).ToList() ?? new List<PostDb>();
                if (!postDbServers.Any())
                {
                    return true;
                }
                for (int i = 0; i < postUrlModels.Count(); i++)
                {
                    var postUrlModel = postUrlModels.ToList()[i];
                    if (!string.IsNullOrEmpty(postUrlModel.Slug) && !string.IsNullOrEmpty(postUrlModel.Titlte) && !string.IsNullOrEmpty(postUrlModel.Url))
                    {
                        var isDuplicatePost = IsDuplicatePost(request, postDbServers, postUrlModel);
                        if (!isDuplicatePost)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private bool IsDuplicatePost(SiteConfigDb request, IEnumerable<PostDb> postServers, LinkModel linkPost)
        {
            if (request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost || postServers.Any())
            {
                var postExist = postServers.FirstOrDefault(pdb =>
                                            (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug.Trim().ToLower() == linkPost.Slug.Trim().ToLower() : true) &&
                                            (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte.Trim().ToLower() == linkPost.Titlte.Trim().ToLower() : true)
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
                                            (request.BasicSetting.CheckDuplicateTitleChap ? pdb.Titlte.ToLower() == linkChap.Titlte.ToLower() : true)
                                        );
                return chapExist != null;
            }

            return false;
        }
    }
}
