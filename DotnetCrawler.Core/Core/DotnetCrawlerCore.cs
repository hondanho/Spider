using Amazon.Runtime.Internal;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public class DotnetCrawlerCore<T> : IDotnetCrawlerCore<T> where T : class
    {
        public SiteConfigDb Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;

        public DotnetCrawlerCore(IMongoRepository<PostDb> postDbRepository, IMongoRepository<ChapDb> chapDbRepository, IMongoRepository<CategoryDb> categoryDbRepository)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
        }

        public DotnetCrawlerCore<T> AddRequest(SiteConfigDb request)
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

        public async Task Crawle()
        {
            // set collection save
            _postDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            var linkReader = new DotnetCrawlerPageLinkReader(Request);

            // get data category
            var htmlDocumentCategory = await Downloader.Download(Request.CategorySetting.Url);
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

            // get list post
            GetPost(linkReader, htmlDocumentCategory, category);
        }

        public async Task ReCrawle()
        {

        }

        private async void GetPost(DotnetCrawlerPageLinkReader linkReader, HtmlDocument htmlDocumentCategory, CategoryDb category)
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

                foreach (var linkPost in linksPost.Take(1)) ///fake
                {
                    // check post duplicate
                    string idPostString = string.Empty;
                    var isDuplicate = IsDuplicatePost(Request, postServers, linkPost, ref idPostString);
                    if (!Request.PostSetting.IsHasChapter && isDuplicate)
                    {
                        Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}", idPostString, linkPost.Titlte, linkPost.Slug));
                        continue;
                    }

                    var htmlDocumentPost = await Downloader.Download(linkPost.Url);
                    var post = await (new CrawlerProcessor(Request).PostProcess(category.IdString, linkPost.Url, htmlDocumentPost));

                    if (!isDuplicate)
                    {
                        await _postDbRepository.InsertOneAsync(post);
                        Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}, Slug: {2}", post.IdString, post.Titlte, post.Slug));
                    } else
                    {
                        post.IdString = idPostString;
                        Console.WriteLine(string.Format("Post existed: Title: {1}, Slug: {2}", post.IdString, linkPost.Titlte, linkPost.Slug));
                    }

                    // get info chap
                    if (Request.PostSetting.IsHasChapter)
                    {
                        GetChap(linkReader, htmlDocumentPost, post);
                    }
                }

                var urlCategoryPostNext = (await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.PagingSelector)).FirstOrDefault();
                if (string.IsNullOrEmpty(urlCategoryPostNext))
                    break;
                htmlDocumentCategory = await Downloader.Download(urlCategoryPostNext);
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

                foreach (var linkChap in linksChap.Take(1)) /// fake
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

                break; /// fake
            }
        }

        private bool IsDuplicatePost(SiteConfigDb request, IEnumerable<PostDb> postServers, LinkModel linkPost, ref string idPostString)
        {
            if (request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost || postServers.Any())
            {
                var postExist = postServers.FirstOrDefault(pdb =>
                                            (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == linkPost.Slug : true) &&
                                            (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == linkPost.Titlte : true)
                                        );
                idPostString = postExist?.IdString ?? string.Empty;
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
