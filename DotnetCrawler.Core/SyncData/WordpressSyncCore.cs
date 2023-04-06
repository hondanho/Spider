using Amazon.Runtime.Internal;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Downloader;
using DotnetCrawler.Processor;
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
using Newtonsoft.Json;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Entity.Wordpress;

namespace DotnetCrawler.Core
{
    public class WordpressSyncCore : IWordpressSyncCore
    {
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IRabitMQProducer _rabitMQProducer;
        private string WordpressUriApi { get; set; }
        private string WordpressUserName { get; set; }
        private string WordpressPassword { get; set; }

        public WordpressSyncCore(
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

            WordpressUriApi = configuration.GetValue<string>("Setting:WordpressUriApi");
            WordpressUserName = configuration.GetValue<string>("Setting:WordpressUserName");
            WordpressPassword = configuration.GetValue<string>("Setting:WordpressPassword");
        }

        public async Task SyncDataBySite(SiteConfigDb request)
        {
            // set collection save
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(request.BasicSetting.Document);

            // init wordpress core
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? WordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? WordpressUserName,
                request.BasicSetting.WordpressPassword ?? WordpressPassword
            );

            // sync category
            var categoryDbsNotSync = _categoryDbRepository.FilterBy(cdb => !cdb.IsSynced).ToList();
            if (categoryDbsNotSync.Any())
            {
                foreach (var categoryDb in categoryDbsNotSync)
                {
                    await wpClient.Categories.CreateAsync(new Category()
                    {
                        Slug = categoryDb.Slug,
                        Name = categoryDb.Titlte,
                        Taxonomy = "category"
                    });
                }
            }

            // sync post
            var categoryWps = (await wpClient.Categories.GetAllAsync()).ToList();
            var postDbs = _postDbRepository.AsQueryable().ToList();
            if (postDbs.Any())
            {
                List<List<PostDb>> postDbsNotSync = postDbs.Where(pdb => !pdb.IsSynced).Select((value, index) => new { Value = value, Index = index })
                    .GroupBy(x => x.Index / 50)
                    .Select(group => group.Select(x => x.Value).ToList())
                    .ToList();

                foreach (var postDbNotSync in postDbsNotSync)
                {
                    var lstCategory = categoryWps.Where(cwp => postDbNotSync.Any(pdb => pdb.CategorySlug == cwp.Slug)).ToList();
                    _rabitMQProducer.SendMessage<PostSyncMessage>(QueueName.QueueSyncPost, new PostSyncMessage()
                    {
                        PostDbs = postDbNotSync,
                        SiteConfigDb = request,
                        Categories = lstCategory
                    });
                }
            }

            var post = await wpClient.Posts.CreateAsync(new Post()
            {
                Title = new Title("test 44/4"),
                Slug = "test"
            });

            var chap = await wpClient.CustomRequest.CreateAsync<Chap, Chap>("/wp-json/wp/v2/chap", new Chap()
            {
                Title = new Title("chap 2"),
                Slug = "chap-1",
                Parent = post.Id,
                Type = "chap",
                Content = new Content("fdsaf")
            });

            // sync all post in category



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

        public async Task JobSyncPost(PostSyncMessage postSyncMessage)
        {
            var request = postSyncMessage.SiteConfigDb;
            var postDbs = postSyncMessage.PostDbs;
            var categories = postSyncMessage.Categories;

            // set collection save
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(request.BasicSetting.Document);

            // init wordpress core
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? WordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? WordpressUserName,
                request.BasicSetting.WordpressPassword ?? WordpressPassword
            );

            foreach (var postDb in postDbs)
            {
                var categoryIds = categories.Where(cwp => cwp.Slug == postDb.Slug)?.Select(cwp => cwp.Id).ToList();
                await wpClient.Posts.CreateAsync(new Post()
                {
                    Title = new Title(postDb.Titlte),
                    Slug = postDb.Slug,
                    Categories = categoryIds,
                    Content = new Content(postDb.Description),
                    Status = Status.Publish,
                    //Tags = ,
                    //Author = ,
                    FeaturedMedia = 
                });
            }
            
        }


        //public async Task<List<WordPressPCL.Models.Post>> GetPostByNames(int categoryId, List<string> postTitles, int take, int skip)
        //{
        //    var queryBuilder = new PostsQueryBuilder()
        //    {
        //        PerPage = take,
        //        Offset = skip,
        //        OrderBy = PostsOrderBy.Date,
        //        Order = Order.DESC,
        //        Search = postTitles.Join(","),
        //        Categories = new List<int> { categoryId }
        //    };

        //    var listPosts = await WordPressClient.Posts.QueryAsync(queryBuilder);
        //    return listPosts.ToList();
        //}

        public async Task InsertManyPostDbToWP(List<PostDb> posts)
        {
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
    }
}
