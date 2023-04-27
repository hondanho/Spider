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
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IRabitMQProducer rabitMQProducer,
            IConfiguration configuration,
            IMongoRepository<CategoryDb> categoryDbRepository)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;
            databaseName = configuration.GetValue<string>("MongoDbSettings:DatabaseName");
        }

        /// <summary>
        /// Save catgegory Data, add url category to category-queue
        /// </summary>
        /// <param name="isUpdatePostChap"></param>
        /// <returns></returns>
        public async Task<bool> Crawle(bool isUpdatePostChap = false)
        {

            var isRerawler = true;
            var siteConfigs = _siteConfigDbRepository.AsQueryable().ToList();

            if (siteConfigs.Any())
            {
                foreach (var siteConfig in siteConfigs)
                {
                    var isCrawler = await _crawlerCore.Crawle(siteConfig, false);
                    if (isCrawler)
                    {
                        isRerawler = false;
                        break;
                    }
                }
            }

            if (isRerawler)
            {
                foreach (var siteConfig in siteConfigs)
                {
                    await _crawlerCore.Crawle(siteConfig, true);
                }
            }

            var result = false;
            SetDocumentDb(siteConfigDb.BasicSetting.Document);

            siteConfigDb.BasicSetting.IsUpdatePostChap = isUpdatePostChap;
            var categoryModels = siteConfigDb.CategorySetting.CategoryModels;

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
                        if (categoryDb == null)
                        {
                            categoryDb = new CategoryDb()
                            {
                                Slug = slugCategory,
                                Titlte = category.Titlte
                            };
                            await _categoryDbRepository.InsertOneAsync(categoryDb);
                            Console.WriteLine(string.Format("CATEGORY NEW: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));
                            _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCrawleCategory, new CategoryMessage()
                            {
                                UrlCategoryCrawleNext = category.Url,
                                CategoryDb = categoryDb,
                                SiteConfigDb = siteConfigDb
                            });
                            result = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine(string.Format("CATEGORY EXIST: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                            // check la update post chap
                            var urlCategoryCrawleNext = isUpdatePostChap &&
                                !string.IsNullOrEmpty(categoryDb.UrlCrawlePostPagingLatest) &&
                                !siteConfigDb.PostSetting.IsHasChapter
                                                            ? categoryDb.UrlCrawlePostPagingLatest : category.Url;
                            _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCrawleCategory, new CategoryMessage()
                            {
                                UrlCategoryCrawleNext = urlCategoryCrawleNext,
                                CategoryDb = categoryDb,
                                SiteConfigDb = siteConfigDb
                            });
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Downloader url category, add next url category to category-queue, add url post to post-queue
        /// </summary>
        /// <param name="categoryMessage"></param>
        public async Task JobCategory(CategoryMessage categoryMessage)
        {
            var request = categoryMessage.SiteConfigDb;
            var categoryUrl = categoryMessage.UrlCategoryCrawleNext;
            var CategorySlug = categoryMessage.CategoryDb.Slug;
            var domain = categoryMessage.SiteConfigDb.BasicSetting.Domain;
            var isUpdatePostChap = request.BasicSetting.IsUpdatePostChap;
            var document = request.BasicSetting.Document ?? databaseName;

            SetDocumentDb(document);

            var htmlDocumentCategory = await DotnetCrawlerDownloader.Download(
                    categoryUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var postUrlModels = await DotnetCrawlerPageLinkReader.GetPageLinkModel(htmlDocumentCategory, request.CategorySetting.LinkPostSelector, domain);
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
                            pdb.CategorySlug == CategorySlug &&
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
                    // check completed then continue
                    if (isUpdatePostChap && postDb.Status == PostStatus.Completed) continue;

                    // check la update post chap
                    if (isUpdatePostChap &&
                        !string.IsNullOrEmpty(postDb?.UrlCrawlePostPagingLatest) &&
                        request.PostSetting.IsHasChapter)
                    {
                        postUrlModel.Url = postDb.UrlCrawlePostPagingLatest;
                    }

                    var index = (pageCategoryNumber - 1) * request.CategorySetting.AmountPostInCategory + (i + 1);

                    _rabitMQProducer.SendMessage<PostMessage>(QueueName.QueueCrawlePost, new PostMessage()
                    {
                        SiteConfigDb = request,
                        CategorySlug = CategorySlug,
                        LinkPost = postUrlModel,
                        IsDuplicate = isDuplicatePost,
                        StatusPost = postDb?.Status ?? string.Empty,
                        Index = index
                    });
                }
            }

            var urlCategoryNext = (await DotnetCrawlerPageLinkReader.GetLinks(htmlDocumentCategory, request.CategorySetting.PagingSelector, domain)).FirstOrDefault();
            if (string.IsNullOrEmpty(urlCategoryNext))
            {
                return;
            }

            _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCrawleCategory, new CategoryMessage()
            {
                UrlCategoryCrawleNext = urlCategoryNext,
                SiteConfigDb = request,
                CategoryDb = categoryMessage.CategoryDb
            });

            if (!request.PostSetting.IsHasChapter)
            {
                categoryMessage.CategoryDb.UrlCrawlePostPagingLatest = urlCategoryNext;
                await _categoryDbRepository.ReplaceOneAsync(categoryMessage.CategoryDb);
            }
        }

        /// <summary>
        /// Downloader post data and save, add url post to queue
        /// </summary>
        /// <param name="postMessage"></param>
        public async Task JobPost(PostMessage postMessage)
        {
            var request = postMessage.SiteConfigDb;
            var categorySlug = postMessage.CategorySlug;
            var linkPostModel = postMessage.LinkPost;
            var isDuplicate = postMessage.IsDuplicate;
            var index = postMessage.Index;
            var status = postMessage.StatusPost;
            var document = request.BasicSetting.Document ?? databaseName;
            SetDocumentDb(document);

            if (!request.PostSetting.IsHasChapter && isDuplicate)
            {
                Console.WriteLine(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
                return;
            }

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    linkPostModel.Url,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var post = await CrawlerProcessor.PostProcess(request, categorySlug, linkPostModel.Url, index, htmlDocumentPost, document);

            if (!isDuplicate)
            {
                await _postDbRepository.InsertOneAsync(post);
                Console.WriteLine(string.Format("POST NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}, Date: {4}", post.Id, post.Index, post.Titlte, post.Slug, DateTime.Now));
            }
            else if (post.Status != status)
            {
                Console.WriteLine(string.Format("POST UPDATED: Title: {0}, Slug: {1}, Date: {2}", linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
            } else
            {
                Console.WriteLine(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
            }

            // get info chap
            if (request.PostSetting.IsHasChapter && !(post.Status == PostStatus.Completed && status == PostStatus.Completed))
            {
                _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueueCrawlePostDetail, new PostDetailMessage()
                {
                    SiteConfigDb = request,
                    PostDb = post,
                    UrlPostCrawleNext = linkPostModel.Url
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
                    pdb.PostSlug == postSlug &&
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
                    _rabitMQProducer.SendMessage<ChapMessage>(QueueName.QueueCrawleChap, new ChapMessage()
                    {
                        SiteConfigDb = request,
                        PostSlug = postSlug,
                        ChapUrl = chapUrlModel.Url,
                        Index = index
                    });
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

            postDetailMessage.PostDb.UrlCrawlePostPagingLatest = postUrlNext;
            await _postDbRepository.ReplaceOneAsync(postDetailMessage.PostDb);
        }

        public async Task JobChap(ChapMessage chapMessage)
        {
            var request = chapMessage.SiteConfigDb;
            SetDocumentDb(request.BasicSetting.Document);

            var postSlug = chapMessage.PostSlug;
            var chapUrl = chapMessage.ChapUrl;
            var index = chapMessage.Index;

            var htmlDocumentChap = await DotnetCrawlerDownloader.Download(
                    chapUrl,
                    downloadPath,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chap = await CrawlerProcessor.ChapProcess(request, postSlug, chapUrl, index, htmlDocumentChap);
            await _chapDbRepository.InsertOneAsync(chap);
            Console.WriteLine(string.Format("CHAP NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}", chap.Id, chap.Index, chap.Titlte, chap.Slug));
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
