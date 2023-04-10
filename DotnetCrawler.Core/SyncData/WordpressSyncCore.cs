using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL.Models;
using WordPressPCL;
using MongoDB.Driver;
using Microsoft.EntityFrameworkCore.Internal;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.Entity.Wordpress;
using DotnetCrawler.Data.Entity.Mongo.Log;
using System.IO;

namespace DotnetCrawler.Core
{
    public class WordpressSyncCore : IWordpressSyncCore
    {

        private readonly IMongoRepository<PostLog> _postLogRepository;
        private readonly IMongoRepository<ChapLog> _chapLogRepository;

        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IRabitMQProducer _rabitMQProducer;
        private string wordpressUriApi { get; set; }
        private string wordpressUserName { get; set; }
        private string wordpressPassword { get; set; }
        private string databaseName { get; set; }
        private string databaseLog { get; set; }

        public WordpressSyncCore(
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            IRabitMQProducer rabitMQProducer,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IMongoRepository<PostLog> postLogRepository,
            IMongoRepository<ChapLog> chapLogRepository,
            IConfiguration configuration)
        {
            _postLogRepository = postLogRepository;
            _chapLogRepository = chapLogRepository;

            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;

            databaseLog = configuration.GetValue<string>("MongoDbSettings:DatabaseLog");
            databaseName = configuration.GetValue<string>("MongoDbSettings:DatabaseName");
            wordpressUriApi = configuration.GetValue<string>("Setting:WordpressUriApi");
            wordpressUserName = configuration.GetValue<string>("Setting:WordpressUserName");
            wordpressPassword = configuration.GetValue<string>("Setting:WordpressPassword");
        }

        public async Task SyncDataBySite(SiteConfigDb request)
        {
            // init
            var document = request.BasicSetting.Document ?? databaseName;
            _categoryDbRepository.SetCollectionSave(document);
            _postLogRepository.SetCollectionSave(databaseLog);

            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            // sync category
            var categoryDbs = _categoryDbRepository.FilterBy(cdb => request.CategorySetting.CategoryModels.Any(csm => csm.Slug == cdb.Slug)).ToList();
            if (categoryDbs.Any())
            {
                var categoryWps = await wpClient.Categories.GetAllAsync();

                foreach (var categoryDb in categoryDbs)
                {
                    var categoryWp = categoryWps.FirstOrDefault(cwp => cwp.Slug == categoryDb.Slug);
                    if (categoryWp == null)
                    {
                        categoryWp = await wpClient.Categories.CreateAsync(new Category()
                        {
                            Slug = categoryDb.Slug,
                            Name = categoryDb.Titlte,
                            Taxonomy = "category"
                        });
                        Console.WriteLine(string.Format("CATEGORY SYNCED: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));
                    }

                    // sync post
                    var postDbs = _postDbRepository.FilterBy(pdb => pdb.CategorySlug == categoryDb.Slug).ToList();
                    if (postDbs.Any())
                    {
                        var postLogs = _postLogRepository.FilterBy(pl => pl.CategorySlug == categoryDb.Slug).ToList();
                        foreach (var postDb in postDbs)
                        {
                            var postLog = postLogs.FirstOrDefault(pl => pl.Slug == postDb.Slug && pl.PostId > 0);
                            _rabitMQProducer.SendMessage<PostSyncMessage>(QueueName.QueueSyncPost, new PostSyncMessage()
                            {
                                PostLog = postLog,
                                PostDb = postDb,
                                CategoryIds = new List<int> { categoryWp.Id },
                                SiteConfigDb = request,
                                CategorySlug = categoryDb.Slug,
                            });
                        }
                    }
                }
            }
        }

        public async Task JobSyncPost(PostSyncMessage postSyncMessage)
        {
            // set init
            var request = postSyncMessage.SiteConfigDb;
            var document = request.BasicSetting.Document ?? databaseName;
            var postDb = postSyncMessage.PostDb;
            var postLog = postSyncMessage.PostLog;
            var categoryWpIds = postSyncMessage.CategoryIds;
            var categorySlug = postSyncMessage.CategorySlug;
            _postDbRepository.SetCollectionSave(document);
            _chapDbRepository.SetCollectionSave(document);
            _postLogRepository.SetCollectionSave(databaseLog);
            _chapLogRepository.SetCollectionSave(databaseLog);
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            if (postLog == null)
            {
                int? featureId = 0;
                if (!string.IsNullOrEmpty(postDb.Avatar))
                {
                    try
                    {
                        var fileFeatureName = Path.GetFileName(postDb.Avatar);
                        var feature = await wpClient.Media.CreateAsync(postDb.Avatar, fileFeatureName);
                        featureId = feature.Id;
                    } catch (Exception ex)
                    {
                    }
                }
                
                var postWp = await wpClient.Posts.CreateAsync(new Post
                {
                    Title = new Title(postDb.Titlte),
                    Slug = postDb.Slug,
                    Categories = categoryWpIds,
                    Content = new Content(postDb.Description),
                    Status = Status.Publish,
                    //Tags = ,
                    //Author = ,
                    FeaturedMedia = featureId
                });

                postLog = new PostLog
                {
                    CategorySlug = categorySlug,
                    PostId = postWp.Id,
                    Slug = postDb.Slug
                };
                await _postLogRepository.InsertOneAsync(postLog);
                Console.WriteLine(string.Format("POST SYNCED: Id: {0}, Slug: {0}, Title: {1}", postDb.Id, postDb.Slug, postDb.Titlte));
            }
            var chapsLogs = _chapLogRepository.FilterBy(cwp => cwp.PostSlug == postDb.Slug && cwp.ChapId > 0).ToList();
            var chapsDbsNotSync = _chapDbRepository.FilterBy(cdb => cdb.PostSlug == postDb.Slug)
                                            .Where(cdb =>
                                                !chapsLogs.Any(cwp => cwp.PostSlug == postDb.Slug)
                                            ).ToList();
            if (chapsDbsNotSync.Any())
            {
                foreach (var chapDb in chapsDbsNotSync)
                {
                    _rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage()
                    {
                        PostWpId = postLog.PostId,
                        SiteConfigDb = request,
                        ChapDb = chapDb
                    });
                }
            }
        }

        public async Task JobSyncChap(ChapSyncMessage chapSyncMessage)
        {
            var request = chapSyncMessage.SiteConfigDb;
            var document = request.BasicSetting.Document ?? databaseName;
            var postWpId = chapSyncMessage.PostWpId;
            var chapDb = chapSyncMessage.ChapDb;
            _chapLogRepository.SetCollectionSave(databaseLog);

            // set init
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            var newChapWp = new Chap
            {
                Content = new Content(chapDb.Content),
                Parent = postWpId,
                Slug = chapDb.Slug,
                Type = "chap",
                Title = new Title(chapDb.Titlte),
                Status = Status.Publish
            };
            var chapWpSynced = await wpClient.CustomRequest.CreateAsync<Chap, Chap>("/wp-json/wp/v2/chap", newChapWp);
            var newChapLog = new ChapLog
            {
                Slug = chapDb.Slug,
                PostSlug = chapDb.PostSlug,
                ChapId = chapWpSynced.Id
            };
            await _chapLogRepository.InsertOneAsync(newChapLog);
            Console.WriteLine(string.Format("CHAP SYNCED: Id: {0}, Slug: {0}, Title: {1}", chapDb.Id, chapDb.Slug, chapDb.Titlte));
        }
    }
}
