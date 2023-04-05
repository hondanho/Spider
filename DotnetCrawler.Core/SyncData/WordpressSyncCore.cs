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
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using WordPressPCL.Models;
using WordPressPCL;
using WordPressPCL.Utility;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore.Internal;
using WordPressPCL.Client;
using Microsoft.Extensions.Hosting;

namespace DotnetCrawler.Core
{
    public class WordpressSyncCore : IWordpressSyncCore
    {
        public IDotnetCrawlerScheduler Scheduler { get; private set; }
        public SiteConfigDb Request { get; private set; }
        public WordPressClient WordPressClient { get; private set; }
        private int taskCount { get; set; } // Số lượng task cần thực thi

        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;

        public WordpressSyncCore(
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IConfiguration configuration)
        {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            taskCount = Int32.Parse(configuration.GetValue<string>("Setting:CrawlerTaskCount"));
        }

        public WordpressSyncCore AddRequest(SiteConfigDb request)
        {
            Request = request;
            return this;
        }

        public WordpressSyncCore AddWordpressClient(WordPressClient wordPressClient, string username, string password)
        {
            WordPressClient = wordPressClient;
            WordPressClient.Auth.UseBasicAuth(username, password);
            return this;
        }

        public WordpressSyncCore AddScheduler(IDotnetCrawlerScheduler scheduler)
        {
            Scheduler = scheduler;
            return this;
        }

        public async Task SyncAllData()
        {
            // set collection save
            _postDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(Request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(Request.BasicSetting.Document);

            // sync all category
            var categorysDb = _categoryDbRepository.AsQueryable().ToList();
            if (!categorysDb.Any()) return;
            var categorysWP = await WordPressClient.Categories.GetAllAsync();
            var createCategoryTasks = categorysDb.Where(ct => !categorysWP.Any(cWP => cWP.Slug == ct.Slug))
                .Select(post => WordPressClient.Categories.CreateAsync(new Category() {
                    Slug = post.Slug,
                    Name = post.Titlte,
                    Taxonomy = "category"
                }))
                .ToList();
            await Task.WhenAll(createCategoryTasks);

            // sync all post in category
            var categorysWPNew = await WordPressClient.Categories.GetAllAsync();
            var allPost = await WordPressClient.Posts.GetAllAsync();

            await WordPressClient.Posts.CreateAsync(new Post()
            {
                Title = new Title("test 44/4")
            });

            //if (category != null)
            //{
            //    var listPostMongo = _postDbRepository.FilterBy(pdb => pdb.CategoryId == category.Id).ToList();
            //    if (listPostMongo.Any())
            //    {
            //        foreach (var postDb in listPostMongo)
            //        {
            //            await InsertManyPost(listPostMongo);
            //        }
            //    }
            //}
        }

        public async Task<List<WordPressPCL.Models.Category>> GetAllCategory()
        {
            var result = await WordPressClient.Categories.GetAllAsync();
            return result.ToList();
        }

        public async Task<List<WordPressPCL.Models.Post>> GetPostByNames(int categoryId, List<string> postTitles, int take, int skip)
        {
            var queryBuilder = new PostsQueryBuilder()
            {
                PerPage = take,
                Offset = skip,
                OrderBy = PostsOrderBy.Date,
                Order = Order.DESC,
                Search = postTitles.Join(","),
                Categories = new List<int> { categoryId }
            };

            var listPosts = await WordPressClient.Posts.QueryAsync(queryBuilder);
            return listPosts.ToList();
        }

        public async Task InsertManyPostDbToWP(List<PostDb> posts) {
            //if (posts.Any())
            //{

            //    // Create tasks to create posts
            //    var createPostTasks = posts
            //        .Select(post => WordPressClient.Posts.CreateAsync(post))
            //        .ToList();

            //    // Wait for all tasks to complete
            //    await Task.WhenAll(createPostTasks);
            //}
        }

        //public int CountChapByPostId(int postId)
        //{

        //}

        ////Task InsertManyChap(List<Data.Entity.Wordpress.Chap> posts);

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="uri">https://YourDomain.com/wp-json/</param>
        ///// <param name="dataObj"></param>
        ///// <returns></returns>
        //public static async Task CreateOrUpdatePost(string uri, string username, string password, DataObject dataObj)
        //{
        //    // Get valid WordPress Client
        //    WordPressClient wpClient = new WordPressClient(uri);

        //    //Basic Auth
        //    wpClient.Auth.UseBasicAuth(username, password);

        //    //Create and Set Post object
        //    var post = new Post
        //    {
        //        Title = new Title(dataObj.Title),
        //        Meta = new Description(dataObj.Description),
        //        Excerpt = new Excerpt(dataObj.Excerpt),
        //        Content = new Content(dataObj.Content),
        //        //slug should be in lower case with hypen(-) separator 
        //        Slug = dataObj.Slug
        //    };

        //    // Assign one or more Categories, if any
        //    if (dataObj.Categories.Count > 0)
        //    {
        //        post.Categories = dataObj.Categories;
        //    }

        //    // Assign one or more Tags, if any
        //    if (dataObj.Tags.Count > 0)
        //    {
        //        post.Tags = dataObj.Tags;
        //    }

        //    if (dataObj.PostId == 0)
        //    {
        //        // if you want to hide comment section
        //        post.CommentStatus = OpenStatus.Closed;
        //        // Set it to draft section if you want to review and then publish
        //        post.Status = Status.Draft;
        //        // Create and get new the post id
        //        dataObj.PostId = wpClient.Post.CreateAsync(post).Result.Id;

        //        // read Note section below - Why update the Post again?
        //        await wpClient.Posts.UpdateAsync(post);
        //    }
        //    else
        //    {
        //        // check the status of post (draft or publish) and then update
        //        if (IsPostDraftStatus(wpClient, dataObj.PostId))
        //        {
        //            post.Status = Status.Draft;
        //        }

        //        await wpClient.Posts.UpdateAsync(post);
        //    }
        //}

        //private static bool IsPostDraftStatus(WordPressClient client, int postId)
        //{
        //    var result = client.Posts.GetByIDAsync(postId, true, true).Result;

        //    if (result.Status == Status.Draft)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
    }
}
