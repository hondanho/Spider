using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using System;
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
                var linksPost = await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.LinkPostSelector);
                if (!linksPost.Any())
                    break;

                foreach (var urlPost in linksPost.Take(1))
                { ///fake
                    if (string.IsNullOrEmpty(urlPost))
                        continue;

                    // get info chap
                    var htmlDocumentPost = await Downloader.Download(urlPost);
                    var post = await (new CrawlerProcessor(Request).PostProcess(category.IdString, urlPost, htmlDocumentPost));

                    // check post duplicate
                    var postIdString = string.Empty;
                    if (IsDuplicatePost(Request, category.IdString, post, idString: ref postIdString))
                    {
                        post.IdString = postIdString;
                        Console.WriteLine(string.Format("Post existed: Id: {0}, Title: {1}, Slug: {2}", post.IdString, post.Titlte, post.Slug));
                    }
                    else
                    {
                        await _postDbRepository.InsertOneAsync(post);
                        Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}, Slug: {2}", post.IdString, post.Titlte, post.Slug));
                    }

                    if (Request.PostSetting.IsHasChapter)
                    {
                        // get info chap
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
                var linksChap = await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.LinkChapSelector);
                if (!linksChap.Any())
                    break;

                foreach (var urlChap in linksChap)
                {
                    if (string.IsNullOrEmpty(urlChap))
                        continue;

                    var htmlDocumentChap = await Downloader.Download(urlChap);
                    var chap = await (new CrawlerProcessor(Request).ChapProcess(post.IdString, urlChap, htmlDocumentChap));

                    // check chap duplicate
                    if (IsDuplicateChap(Request, post.IdString, chap))
                    {
                        Console.WriteLine(string.Format("Chap existed: Title: {0}, Slug: {1}", chap.Titlte, chap.Slug));
                    }
                    else
                    {
                        await _chapDbRepository.InsertOneAsync(chap);
                        Console.WriteLine(string.Format("Chap new: Title: {0}, Slug: {1}", chap.Titlte, chap.Slug));
                    }
                }

                var urlPostChapNext = (await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.PagingSelector)).FirstOrDefault();
                if (string.IsNullOrEmpty(urlPostChapNext))
                    break;
                htmlDocumentPost = await Downloader.Download(urlPostChapNext);
            }
        }


        private bool IsDuplicatePost(SiteConfigDb request, string categoryId, PostDb post, ref string idString)
        {
            if (request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost)
            {
                Expression<Func<PostDb, bool>> condition = pdb => pdb.CategoryId == categoryId &&
                                        (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == post.Slug : true) &&
                                        (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == post.Titlte : true);
                var postDuplicate = _postDbRepository.FindOne(condition);
                idString = postDuplicate?.IdString;
                return postDuplicate != null;
            }

            return false;
        }

        private bool IsDuplicateChap(SiteConfigDb request, string postId, ChapDb chap)
        {
            if (request.BasicSetting.CheckDuplicateTitleChap || request.BasicSetting.CheckDuplicateSlugChap)
            {
                Expression<Func<ChapDb, bool>> condition = pdb => pdb.PostId == postId &&
                                        (request.BasicSetting.CheckDuplicateSlugChap ? pdb.Slug == chap.Slug : true) &&
                                        (request.BasicSetting.CheckDuplicateTitleChap ? pdb.Titlte == chap.Titlte : true);
                var chapDuplicate = _chapDbRepository.FindOne(condition);
                return chapDuplicate != null;
            }

            return false;
        }
    }
}
