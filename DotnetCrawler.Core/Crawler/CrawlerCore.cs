using Amazon.Runtime.Internal;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using DotnetCrawler.Scheduler;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver.Core.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using WordPressPCL.Models;

namespace DotnetCrawler.Core {
    public class CrawlerCore<T> : ICrawlerCore<T> where T : class {
        public SiteConfigDb Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        public DotnetCrawlerPageLinkReader LinkReader { get; private set; }
        private int crawlerTaskCount { get; set; }

        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IRabitMQProducer _rabitMQProducer;

        public CrawlerCore(
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IRabitMQProducer rabitMQProducer,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IConfiguration configuration) {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;
            crawlerTaskCount = configuration.GetValue<int>("Setting:CrawlerTaskCount");
        }

        public CrawlerCore<T> AddRequest(SiteConfigDb request) {
            Request = request;
            LinkReader = new DotnetCrawlerPageLinkReader(Request);
            return this;
        }

        public CrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader) {
            Downloader = downloader;
            return this;
        }

        public CrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler) {
            Scheduler = scheduler;
            return this;
        }

        /// <summary>
        /// Save catgegory Data, add url category to category-queue
        /// </summary>
        /// <param name="isReCrawleSmall"></param>
        /// <returns></returns>
        public async Task Crawle(bool isReCrawleSmall = false) {
            SetDocumentDb(Request.BasicSetting.Document);

            // add worker categoryDb
            if(Request.CategorySetting.CategoryModels.Any()) {
                var categoryDbs = _categoryDbRepository.FilterBy(cdb => Request.CategorySetting.CategoryModels.Any(cgm => cgm.Slug == cdb.Slug));

                foreach(var category in Request.CategorySetting.CategoryModels.Take(1)) { //fake
                    if(!string.IsNullOrEmpty(category?.Slug) && !string.IsNullOrEmpty(category?.Titlte) && !string.IsNullOrEmpty(category?.Url)) {
                        var categoryDb = categoryDbs.FirstOrDefault(cdb => cdb.Slug == category.Slug);
                        if(categoryDb == null) {
                            categoryDb = new CategoryDb() {
                                Slug = category.Slug,
                                Titlte = category.Titlte
                            };
                            await _categoryDbRepository.InsertOneAsync(categoryDb);
                            Console.WriteLine(string.Format("Category new: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                        } else {
                            Console.WriteLine(string.Format("Category existed: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte));
                        }

                        _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCategoryName, new CategoryMessage() {
                            CategoryUrl = category.Url,
                            CategoryIdString = categoryDb.IdString,
                            BaseMessage = new BaseMessage() {
                                Request = Request,
                                Downloader = Downloader,
                                LinkReader = LinkReader
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Downloader url category, add next url category to category-queue, add url post to post-queue
        /// </summary>
        /// <param name="categoryMessage"></param>
        public async void JobCategory(CategoryMessage categoryMessage) {
            SetDocumentDb(categoryMessage.BaseMessage.Request.BasicSetting.Document);
            var request = categoryMessage.BaseMessage.Request;
            var linkReader = categoryMessage.BaseMessage.LinkReader;
            var downloader = categoryMessage.BaseMessage.Downloader;

            var categoryUrl = categoryMessage.CategoryUrl;
            var categoryIdString = categoryMessage.CategoryIdString;

            var htmlDocumentCategory = await downloader.Download(categoryUrl);
            var postUrlModesl = await linkReader.GetPageLinkModel(htmlDocumentCategory, request.CategorySetting.LinkPostSelector);
            if(!postUrlModesl.Any()) {
                return;
            }

            var postDbServers = _postDbRepository.FilterBy(pdb =>
                            pdb.CategoryId == categoryIdString &&
                            postUrlModesl.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                        );

            foreach(var postUrlModel in postUrlModesl.Take(1)) { //fake
                if(!string.IsNullOrEmpty(postUrlModel.Slug) && !string.IsNullOrEmpty(postUrlModel.Titlte) && !string.IsNullOrEmpty(postUrlModel.Url)) {
                    var isDuplicatePost = IsDuplicatePost(request, postDbServers, postUrlModel);
                    _rabitMQProducer.SendMessage<PostMessage>(QueueName.QueuePostName, new PostMessage() {
                        BaseMessage = categoryMessage.BaseMessage,
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

            var urlCategoryNext = (await linkReader.GetLinks(htmlDocumentCategory, request.CategorySetting.PagingSelector)).FirstOrDefault();
            if(string.IsNullOrEmpty(urlCategoryNext)) {
                return;
            }
            _rabitMQProducer.SendMessage<CategoryMessage>(QueueName.QueueCategoryName, new CategoryMessage() {
                CategoryUrl = urlCategoryNext,
                BaseMessage = categoryMessage.BaseMessage,
                CategoryIdString = categoryMessage.CategoryIdString
            });
        }

        /// <summary>
        /// Downloader post data and save, add url post to queue
        /// </summary>
        /// <param name="postMessage"></param>
        public async void JobPost(PostMessage postMessage) {
            SetDocumentDb(postMessage.BaseMessage.Request.BasicSetting.Document);
            var request = postMessage.BaseMessage.Request;
            var linkReader = postMessage.BaseMessage.LinkReader;
            var downloader = postMessage.BaseMessage.Downloader;

            var categoryIdString = postMessage.CategoryIdString;
            var isReCrawleSmall = postMessage.IsReCrawleSmall;
            var linkPostModel = postMessage.LinkPost;
            var isDuplicate = postMessage.IsDuplicate;
            var postExist = postMessage.PostDb;

            string idPostString = string.Empty;
            if(!request.PostSetting.IsHasChapter && isDuplicate) {
                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
                return;
            }

            var htmlDocumentPost = await downloader.Download(linkPostModel.Url);
            var post = await (new CrawlerProcessor(request).PostProcess(categoryIdString, linkPostModel.Url, htmlDocumentPost));

            if(!isDuplicate) {
                await _postDbRepository.InsertOneAsync(post);
                Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}, Slug: {2}, Date: {3}", post.IdString, post.Titlte, post.Slug, DateTime.Now));
            } else {
                post.IdString = idPostString;
                Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}, Date: {3}", post.IdString, linkPostModel.Titlte, linkPostModel.Slug, DateTime.Now));
            }

            // get info chap
            if(request.PostSetting.IsHasChapter) {
                _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueuePostDetailName, new PostDetailMessage() {
                    BaseMessage = postMessage.BaseMessage,
                    PostIdString = post.IdString,
                    PostUrl = linkPostModel.Url
                });
            }
        }

        /// <summary>
        /// Download url post, add next url post to post-detail-queue, add url chap to queue
        /// </summary>
        /// <param name="postDetailMessage"></param>
        public async void JobPostDetail(PostDetailMessage postDetailMessage) {
            SetDocumentDb(postDetailMessage.BaseMessage.Request.BasicSetting.Document);
            var request = postDetailMessage.BaseMessage.Request;
            var linkReader = postDetailMessage.BaseMessage.LinkReader;
            var downloader = postDetailMessage.BaseMessage.Downloader;

            var postUrl = postDetailMessage.PostUrl;
            var postIdString = postDetailMessage.PostIdString;

            var htmlDocumentPost = await downloader.Download(postUrl);
            var chapUrlModesl = await linkReader.GetPageLinkModel(htmlDocumentPost, request.PostSetting.LinkChapSelector);
            if(!chapUrlModesl.Any()) {
                return;
            }

            var chapDbServers = _chapDbRepository.FilterBy(pdb =>
                        pdb.PostId == postIdString &&
                        chapUrlModesl.Any(lp => lp.Slug == pdb.Slug || lp.Titlte == pdb.Titlte)
                    );

            foreach(var chapUrlModel in chapUrlModesl.Take(1)) { //fake
                if(!string.IsNullOrEmpty(chapUrlModel.Slug) && !string.IsNullOrEmpty(chapUrlModel.Titlte) && !string.IsNullOrEmpty(chapUrlModel.Url)) {
                    var isDuplicateChap = IsDuplicateChap(request, chapDbServers, chapUrlModel);
                    if (isDuplicateChap) {
                        Console.WriteLine(string.Format("Chap existed: Title: {0}, Slug: {1}", chapUrlModel.Titlte, chapUrlModel.Slug));
                        continue;
                    }

                    _rabitMQProducer.SendMessage<ChapMessage>(QueueName.QueueChapName, new ChapMessage() {
                        BaseMessage = postDetailMessage.BaseMessage,
                        PostIdString = postIdString,
                        ChapUrl = chapUrlModel.Url
                    });
                }
            }

            var postUrlNext = (await linkReader.GetLinks(htmlDocumentPost, request.PostSetting.PagingSelector)).FirstOrDefault();
            if(string.IsNullOrEmpty(postUrlNext)) {
                return;
            }

            _rabitMQProducer.SendMessage<PostDetailMessage>(QueueName.QueuePostDetailName, new PostDetailMessage() {
                BaseMessage = postDetailMessage.BaseMessage,
                PostIdString = postIdString,
                PostUrl = postUrlNext
            });
        }

        public async void JobChap(ChapMessage chapMessage) {
            SetDocumentDb(chapMessage.BaseMessage.Request.BasicSetting.Document);
            var request = chapMessage.BaseMessage.Request;
            var linkReader = chapMessage.BaseMessage.LinkReader;
            var downloader = chapMessage.BaseMessage.Downloader;

            var postIdString = chapMessage.PostIdString;
            var chapUrl = chapMessage.ChapUrl;

            var htmlDocumentChap = await downloader.Download(chapUrl);
            var chap = await (new CrawlerProcessor(request).ChapProcess(postIdString, chapUrl, htmlDocumentChap));
            await _chapDbRepository.InsertOneAsync(chap);
            Console.WriteLine(string.Format("Chap new: Title: {0}, Slug: {1}", chap.Titlte, chap.Slug));

            var urlChapNext = (await linkReader.GetLinks(htmlDocumentChap, request.ChapSetting.PagingSelector)).FirstOrDefault();
            if(string.IsNullOrEmpty(urlChapNext)) {
                return;
            }

            _rabitMQProducer.SendMessage<ChapMessage>(QueueName.QueueChapName, new ChapMessage() {
                BaseMessage = chapMessage.BaseMessage,
                PostIdString = postIdString,
                ChapUrl = urlChapNext
            });
        }

        private void SetDocumentDb(string documentName) {
            _postDbRepository.SetCollectionSave(documentName);
            _chapDbRepository.SetCollectionSave(documentName);
            _categoryDbRepository.SetCollectionSave(documentName);
        }

        private bool IsDuplicatePost(SiteConfigDb request, IEnumerable<PostDb> postServers, LinkModel linkPost) {
            if(request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost || postServers.Any()) {
                var postExist = postServers.FirstOrDefault(pdb =>
                                            (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == linkPost.Slug : true) &&
                                            (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == linkPost.Titlte : true)
                                        );
                return postExist != null;
            }

            return false;
        }

        private bool IsDuplicateChap(SiteConfigDb request, IEnumerable<ChapDb> chapServers, LinkModel linkChap) {
            if(request.BasicSetting.CheckDuplicateSlugChap || request.BasicSetting.CheckDuplicateTitleChap || chapServers.Any()) {
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
