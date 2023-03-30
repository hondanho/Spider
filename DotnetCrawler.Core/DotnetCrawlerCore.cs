using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Pipeline;
using DotnetCrawler.Processor;
using DotnetCrawler.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DotnetCrawler.Core {
    public class DotnetCrawlerCore<T> : IDotnetCrawlerCore<T> where T : class {
        public IDotnetCrawlerRequest Request { get; private set; }
        public IDotnetCrawlerDownloader Downloader { get; private set; }
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;

        public DotnetCrawlerCore(IMongoRepository<PostDb> postDbRepository, IMongoRepository<ChapDb> chapDbRepository, IMongoRepository<CategoryDb> categoryDbRepository) {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
        }

        public DotnetCrawlerCore<T> AddRequest(IDotnetCrawlerRequest request) {
            Request = request;
            return this;
        }

        public DotnetCrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader) {
            Downloader = downloader;
            return this;
        }

        public DotnetCrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler) {
            Scheduler = scheduler;
            return this;
        }

        public async Task Crawle() {
            // set collection save
            _postDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(Request.BasicSetting.Document);

            // Check Thu Thap
            if(Request.BasicSetting.IsThuThap == false)
                return;

            // create category
            var category = _categoryDbRepository.FindOne(cdb => cdb.Slug == Request.CategorySetting.Slug);
            if(category == null) {
                category = new CategoryDb() {
                    Slug = Request.CategorySetting.Slug,
                    Titlte = Request.CategorySetting.Titlte
                };
                await _categoryDbRepository.InsertOneAsync(category);
                Console.WriteLine(string.Format("Category new: Id: {0}, Title: {1}", category.Id, category.Titlte));
            } else {
                Console.WriteLine(string.Format("Category đã tồn tại: Id: {0}, Title: {1}", category.Id, category.Titlte));
            }

            // get list chap
            var linkReader = new DotnetCrawlerPageLinkReader(Request);
            var htmlDocumentCategory = await Downloader.Download(Request.CategorySetting.Url);

            while(true) {
                var linksPost = await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.LinkPostSelector);
                if(!linksPost.Any())
                    break;

                foreach(var urlPost in linksPost.Take(1)) { ///fake
                    if(string.IsNullOrEmpty(urlPost))
                        continue;

                    // get info chap
                    var htmlDocumentPost = await Downloader.Download(urlPost);
                    var post = await (new CrawlerProcessor(Request).PostProcess(category.Id.ToString(), urlPost, htmlDocumentPost));

                    // check post duplicate
                    if(IsDuplicatePost(Request, category.Id, post)) {
                        Console.WriteLine(string.Format("Post đã tồn tại: Id: {0}, Title: {1}", post.Id, post.Titlte));
                    } else {
                        await _postDbRepository.InsertOneAsync(post);
                        Console.WriteLine(string.Format("Post new: Id: {0}, Title: {1}", post.Id, post.Titlte));
                    }

                    if(Request.PostSetting.IsHasChapter) {
                        // get info chap
                        while(true) {
                            var linksChap = await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.LinkChapSelector);
                            if(!linksChap.Any())
                                break;

                            foreach(var urlChap in linksChap.Take(1)) { ///fake
                                if(string.IsNullOrEmpty(urlChap))
                                    continue;

                                var htmlDocumentChap = await Downloader.Download(urlChap);
                                var chap = await (new CrawlerProcessor(Request).ChapProcess(post.Id.ToString(), urlChap, htmlDocumentChap));

                                // check chap duplicate
                                if(IsDuplicateChap(Request, chap.Id, chap)) {
                                    Console.WriteLine(string.Format("Chap đã tồn tại: Id: {0}, Title: {1}", chap.Id, chap.Titlte));
                                } else {
                                    await _chapDbRepository.InsertOneAsync(chap);
                                    Console.WriteLine(string.Format("Chap new: Id: {0}, Title: {1}", chap.Id, chap.Titlte));
                                }
                            }

                            var urlPostChapNext = (await linkReader.GetLinks(htmlDocumentPost, Request.PostSetting.PagingSelector)).FirstOrDefault();
                            if(string.IsNullOrEmpty(urlPostChapNext))
                                break;
                            htmlDocumentPost = await Downloader.Download(urlPostChapNext);
                            break; ///fake
                        }
                    }

                }

                var urlCategoryPostNext = (await linkReader.GetLinks(htmlDocumentCategory, Request.CategorySetting.PagingSelector)).FirstOrDefault();
                if(string.IsNullOrEmpty(urlCategoryPostNext))
                    break;
                htmlDocumentCategory = await Downloader.Download(urlCategoryPostNext);
            }
        }

        private bool IsDuplicatePost(IDotnetCrawlerRequest request, object categoryId, PostDb post) {
            if(request.BasicSetting.CheckDuplicateTitlePost || request.BasicSetting.CheckDuplicateSlugPost) {
                Expression<Func<PostDb, bool>> condition = pdb => pdb.CategoryId == categoryId.ToString() &&
                                        (request.BasicSetting.CheckDuplicateSlugPost ? pdb.Slug == post.Slug : true) &&
                                        (request.BasicSetting.CheckDuplicateTitlePost ? pdb.Titlte == post.Titlte : true);
                var postDuplicate = _postDbRepository.FindOne(condition);
                return postDuplicate != null;
            }

            return false;
        }

        private bool IsDuplicateChap(IDotnetCrawlerRequest request, object postId, ChapDb chap) {
            if(request.BasicSetting.CheckDuplicateTitleChap || request.BasicSetting.CheckDuplicateSlugChap) {
                Expression<Func<ChapDb, bool>> condition = pdb => pdb.PostId == postId.ToString() &&
                                        (request.BasicSetting.CheckDuplicateSlugChap ? pdb.Slug == chap.Slug : true) &&
                                        (request.BasicSetting.CheckDuplicateTitleChap ? pdb.Titlte == chap.Titlte : true);
                var chapDuplicate = _chapDbRepository.FindOne(condition);
                return chapDuplicate != null;
            }

            return false;
        }
    }
}
