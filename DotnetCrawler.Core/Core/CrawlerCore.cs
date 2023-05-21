﻿using DotnetCrawler.Base.Extension;
using DotnetCrawler.Core.Extension;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using Microsoft.EntityFrameworkCore.Internal;
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

        public CrawlerCore(
            IRabitMQProducer rabitMQProducer,
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
        }

        /// <summary>
        /// Find next category process
        /// </summary>
        public async Task NextCategory(CategoryModel category = null)
        {
            _siteConfigDbRepository.SetCollectionSave("novelfull");
            _postDbRepository.SetCollectionSave("novelfull");
            _chapDbRepository.SetCollectionSave("novelfull");
            _categoryDbRepository.SetCollectionSave("novelfull");

            if (category == null)
            {
                var siteConfigFirst = _siteConfigDbRepository.FilterBy(item => item.BasicSetting.IsThuThap).FirstOrDefault();
                if (siteConfigFirst == null || siteConfigFirst.CategorySetting.CategoryModels.FirstOrDefault() == null)
                {
                    Helper.Display("Nothing site config", MessageType.Warning);
                }
                else
                {
                    await JobCategoryData(siteConfigFirst.CategorySetting.CategoryModels.FirstOrDefault(), siteConfigFirst);
                }
                return;
            }

            var siteConfig = await _siteConfigDbRepository.FindOneAsync(site => site.CategorySetting.CategoryModels.Any(ctg => ctg.Slug == category.Slug));
            if (siteConfig == null || !siteConfig.CategorySetting.CategoryModels.Any())
            {
                Helper.Display(String.Format("Not found category slug {0}", category.Slug), MessageType.Warning);
                return;
            }
            var categoryIndex = siteConfig.CategorySetting.CategoryModels.IndexOf(
                    siteConfig.CategorySetting.CategoryModels.Where(p => p.Slug == category.Slug).FirstOrDefault()
                );
            if (categoryIndex == siteConfig.CategorySetting.CategoryModels.Count() - 1)
            {
                var allSiteConfig = _siteConfigDbRepository.FilterBy(item => item.BasicSetting.IsThuThapLai).ToList();
                var indexSiter = allSiteConfig.IndexOf(allSiteConfig.Where(item => item.Id == siteConfig.Id).FirstOrDefault());
                if (indexSiter != -1 && indexSiter != allSiteConfig.Count())
                {
                    await NextCategory(allSiteConfig[indexSiter + 1].CategorySetting.CategoryModels.FirstOrDefault());
                    Helper.Display("Next Site", MessageType.Information);
                }
            }
            else
            {
                await JobCategoryData(siteConfig.CategorySetting.CategoryModels[categoryIndex + 1], siteConfig);
            }
        }

        /// <summary>
        /// Insert category
        /// </summary>
        public async Task JobCategoryData(CategoryModel category, SiteConfigDb siteConfig)
        {
            var slugCategory = Helper.CleanSlug(category.Slug);
            var categoryDb = await _categoryDbRepository.FindOneAsync(cdb => cdb.Slug == slugCategory);

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
                Helper.Display(
                    string.Format("CATEGORY NEW: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte),
                    MessageType.Information
                );
            }
            else
            {
                Helper.Display(string.Format("CATEGORY EXIST: Slug: {0}, Title: {1}", categoryDb.Slug, categoryDb.Titlte), MessageType.Information);
            }

            await JobCategoryDetail(siteConfig, category.Url, categoryDb);
        }

        /// <summary> 
        /// Read post from category, push post to queue
        /// </summary>
        public async Task JobCategoryDetail(SiteConfigDb request, string categoryUrl, CategoryDb categoryDb)
        {
            var htmlDocumentCategory = await DotnetCrawlerDownloader.Download(
                    categoryUrl,
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
            var urlCategoryNext = (
                    await DotnetCrawlerPageLinkReader.GetLinks(
                    htmlDocumentCategory,
                    request.CategorySetting.PagingSelector,
                    request.BasicSetting.Domain)
                ).FirstOrDefault();
            // save next url process
            categoryDb.UrlCategoryPagingNext = urlCategoryNext;
            categoryDb.UrlCategoryPagingLatest = categoryUrl;
            await _categoryDbRepository.ReplaceOneAsync(categoryDb);

            var isRunNextCategory = false;
            for (int i = 0; i < postUrlModels.Count(); i++)
            {
                var postUrlModel = postUrlModels.ToList()[i];
                var isDuplicatePost = IsDuplicatePost(request, postDbServers, postUrlModel);
                var postDb = postDbServers.FirstOrDefault(pdb =>
                            (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == postUrlModel.Slug : true) &&
                            (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == postUrlModel.Titlte : true)
                        );

                // check completed then continue
                if (postDb != null && postDb.IsCrawleDone)
                {
                    Helper.Display("Post Skip Because Crawle Completed", MessageType.Information);
                    continue;
                }

                if (!string.IsNullOrEmpty(postDb?.UrlPostPagingCrawleLatest))
                {
                    postUrlModel.Url = postDb.UrlPostPagingCrawleLatest;
                }

                var index = (pageCategoryNumber - 1) * request.CategorySetting.AmountPostInCategory + (i + 1);
                isRunNextCategory = i == (postUrlModels.Count() - 1);
                _rabitMQProducer.SendMessage<PostMessage>(QueueName.QueueCrawlePost, new PostMessage()
                {
                    SiteConfigDb = request,
                    CategorySlug = categoryDb.Slug,
                    LinkPostCrawle = postUrlModel,
                    IsDuplicate = isDuplicatePost,
                    Index = index,
                    IsNextCategory = i == (postUrlModels.Count() - 1)
                });
            }

            if (!isRunNextCategory)
            {
                NextJobPostDetail(new PostDetailMessage
                {
                    CategorySlug = categoryDb.Slug,
                    IsNextCategory = true
                },
                request,
                isNextPost: false);
            }
        }

        /// <summary>
        /// Download and insert data post to database
        /// </summary>
        public async Task JobPostData(PostMessage postMessage)
        {
            var request = postMessage.SiteConfigDb;
            var categorySlug = postMessage.CategorySlug;
            var linkPostCrawle = postMessage.LinkPostCrawle;
            var isDuplicate = postMessage.IsDuplicate;
            var index = postMessage.Index;

            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(request.BasicSetting.Document);

            if (!request.PostSetting.IsHasChapter && isDuplicate)
            {
                Helper.Display(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostCrawle.Titlte, linkPostCrawle.Slug, DateTime.Now), MessageType.Information);
                return;
            }

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    linkPostCrawle.Url,
                    request.BasicSetting.Proxys,
                    request.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var post = await CrawlerProcessor.PostProcess(request, categorySlug, linkPostCrawle.Url, index, htmlDocumentPost, request.BasicSetting.Document);

            if (!isDuplicate)
            {
                await _postDbRepository.InsertOneAsync(post);
                Helper.Display(string.Format("POST NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}, Date: {4}", post.Id, post.Index, post.Titlte, post.Slug, DateTime.Now), MessageType.Information);
            }
            else
            {
                Helper.Display(string.Format("POST EXIST: Title: {0}, Slug: {1}, Date: {2}", linkPostCrawle.Titlte, linkPostCrawle.Slug, DateTime.Now), MessageType.Information);
            }

            // get info chap
            if (request.PostSetting.IsHasChapter)
            {
                await JobPostDetail(new PostDetailMessage()
                {
                    SiteConfigDb = request,
                    PostDb = post,
                    UrlPostCrawleNext = linkPostCrawle.Url,
                    CategorySlug = postMessage.CategorySlug,
                    IsNextCategory = postMessage.IsNextCategory
                });
            }
        }

        /// <summary>
        /// Download url post, add next url post to post-detail-queue, add url chap to queue
        /// </summary>
        public async Task JobPostDetail(PostDetailMessage postDetailMessage)
        {
            var request = postDetailMessage.SiteConfigDb;

            var postUrl = postDetailMessage.UrlPostCrawleNext;
            var postSlug = postDetailMessage.PostDb.Slug;
            var domain = postDetailMessage.SiteConfigDb.BasicSetting.Domain;

            var htmlDocumentPost = await DotnetCrawlerDownloader.Download(
                    postUrl,
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
                var isDuplicateChap = IsDuplicateChap(request, chapDbServers, chapUrlModel);
                if (isDuplicateChap)
                {
                    Helper.Display(string.Format("CHAP EXIST: Title: {0}, Slug: {1}", chapUrlModel.Titlte, chapUrlModel.Slug), MessageType.Information);
                    continue;
                }

                var index = (pagePostNumber - 1) * request.PostSetting.AmountChapInPost + (i + 1);
                await JobChapData(request, postSlug, chapUrlModel.Url, index, postDetailMessage.PostDb);
            }

            var postUrlNext = (await DotnetCrawlerPageLinkReader.GetLinks(htmlDocumentPost, request.PostSetting.PagingSelector, domain)).FirstOrDefault();
            postDetailMessage.PostDb.UrlPostPagingCrawleNext = postUrlNext;
            postDetailMessage.PostDb.UrlPostPagingCrawleLatest = postUrl;
            postDetailMessage.PostDb.IsCrawleDone = (postDetailMessage.PostDb.Status == PostStatus.Completed &&
                                                    chapUrlModels.Count() < request.PostSetting.AmountChapInPost
                                                ) ? true : false;
            await _postDbRepository.ReplaceOneAsync(postDetailMessage.PostDb);

            // next post
            await NextJobPostDetail(
                new PostDetailMessage()
                {
                    SiteConfigDb = request,
                    PostDb = postDetailMessage.PostDb,
                    UrlPostCrawleNext = postDetailMessage.PostDb.UrlPostPagingCrawleNext,
                    IsNextCategory = postDetailMessage.IsNextCategory,
                    CategorySlug = postDetailMessage.CategorySlug
                },
                request,
                isNextPost: chapUrlModels.Count() == request.PostSetting.AmountChapInPost && !string.IsNullOrEmpty(postUrlNext)
                );
        }

        public async Task NextJobPostDetail(
            PostDetailMessage postDetailMessage,
            SiteConfigDb request,
            bool isNextPost
            )
        {
            if (isNextPost)
            {
                await JobPostDetail(postDetailMessage);
            }
            else if (postDetailMessage.IsNextCategory)
            {
                var categoryDb = await _categoryDbRepository.FindOneAsync(item => item.Slug == postDetailMessage.CategorySlug);
                if (!string.IsNullOrEmpty(categoryDb.UrlCategoryPagingNext))
                {
                    await JobCategoryDetail(request, categoryDb.UrlCategoryPagingNext, categoryDb);
                } else
                {
                    var siteConfig = await _siteConfigDbRepository.FindOneAsync(site => site.CategorySetting.CategoryModels.Any(ctg => ctg.Slug == categoryDb.Slug));
                    if (siteConfig != null)
                    {
                        var categoryModel = siteConfig.CategorySetting.CategoryModels.FirstOrDefault(item => item.Slug == categoryDb.Slug);
                        await NextCategory(categoryModel);
                    }
                }
            }
        }

        public async Task JobChapData(SiteConfigDb siteConfigDb, string slugPost, string urlChap, int index, PostDb postDb)
        {
            var htmlDocumentChap = await DotnetCrawlerDownloader.Download(
                    urlChap,
                    siteConfigDb.BasicSetting.Proxys,
                    siteConfigDb.BasicSetting.UserAgent,
                    DotnetCrawlerDownloaderType.FromMemory
                );
            var chap = await CrawlerProcessor.ChapProcess(siteConfigDb, slugPost, urlChap, index, htmlDocumentChap);
            await _chapDbRepository.InsertOneAsync(chap);
            Helper.Display(string.Format("CHAP NEW: Id: {0}, Index: {1}, Title: {2}, Slug: {3}", chap.Id, chap.Index, chap.Titlte, chap.Slug), MessageType.Information);
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
