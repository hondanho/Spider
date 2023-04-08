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
using Microsoft.AspNetCore.Mvc;
using DotnetCrawler.Data.Entity.Mongo.Log;

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
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            // sync category
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList();
            if (categoryDbs.Any())
            {
                var categoryWps = await wpClient.Categories.GetAllAsync();
                foreach (var categoryDb in categoryDbs.Where(cdb => !categoryWps.Any(cwp => cwp.Slug == cdb.Slug)))
                {
                    var categorySynced = await wpClient.Categories.CreateAsync(new Category()
                    {
                        Slug = categoryDb.Slug,
                        Name = categoryDb.Titlte,
                        Taxonomy = "category"
                    });
                    Console.WriteLine(string.Format("CATEGORY SYNCED: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));
                }

                var categorySyncedWps = await wpClient.Categories.GetAllAsync();
                foreach (var categoryWp in categorySyncedWps)
                {
                    _rabitMQProducer.SendMessage<CategorySyncMessage>(QueueName.QueueSyncCategory, new CategorySyncMessage()
                    {
                        CategoryDb = categoryDb,
                        SiteConfigDb = request
                    });
                }
            }
        }

        public async Task JobSyncCategory(CategorySyncMessage categorySyncMessage)
        {
            // set init
            var request = categorySyncMessage.SiteConfigDb;
            var categoryDb = categorySyncMessage.CategoryDb;
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );
            _postLogRepository.SetCollectionSave(databaseLog);

            await wpClient.Categories.CreateAsync(new Category()
            {
                Slug = categoryDb.Slug,
                Name = categoryDb.Titlte,
                Taxonomy = "category"
            });
            Console.WriteLine(string.Format("CATEGORY SYNCED: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));

            // sync post
            var postDbs = _postDbRepository.FilterBy(pdb => pdb.CategorySlug == categoryDb.Slug).ToList();
            if (postDbs.Any())
            {
                var postLogs = _postLogRepository.FilterBy(pl => categoryDb.Slug == pl.CategorySlug).ToList();
                var categoryWps = await wpClient.Categories.GetAllAsync();
                foreach (var postDb in postDbs.Where(pdb => !postLogs.Any(pl => pl.Slug == pdb.Slug)).ToList())
                {
                    _rabitMQProducer.SendMessage<PostSyncMessage>(QueueName.QueueSyncPost, new PostSyncMessage()
                    {
                        PostDb = postDb,
                        CategoryIds = new List<int> { categoryWps.FirstOrDefault(cwp => cwp.Slug == categoryDb.Slug)?.Id ?? 0 },
                        SiteConfigDb = request
                    });
                }

                //foreach (var postDb in postDbsSynced)
                //{
                //    var isExistChapNotSync = _chapDbRepository.AsQueryable().Count(cdb => cdb.PostSlug == postDb.Slug && !cdb.IsSynced);
                //    if (isExistChapNotSync > 0)
                //    {
                //        var chapsNotSync = _chapDbRepository.FilterBy(cdb => !cdb.IsSynced && cdb.PostSlug == postDb.Slug).ToList();
                //        if (chapsNotSync.Any())
                //        {
                //            foreach (var chapDb in chapsNotSync)
                //            { // fake
                //                _rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage()
                //                {
                //                    PostWpId = postDb.IdPostWpSynced,
                //                    SiteConfigDb = request,
                //                    ChapDb = chapDb
                //                });
                //            }

                //            break; // fake
                //        }
                //    }
                //}
            }
        }

        public async Task JobSyncPost(PostSyncMessage postSyncMessage)
        {
            // set init
            var request = postSyncMessage.SiteConfigDb;
            var postDb = postSyncMessage.PostDb;
            var categoryWpIds = postSyncMessage.CategoryIds;
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _postLogRepository.SetCollectionSave(databaseLog);
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            

            var chapsNotSync = _chapDbRepository.FilterBy(cdb => !cdb.IsSynced && cdb.PostSlug == postDb.Slug);
            if (chapsNotSync.Any())
            {
                foreach (var chapDb in chapsNotSync)
                { // fake
                    _rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage()
                    {
                        PostWpId = postWpSynced.Id,
                        SiteConfigDb = request,
                        ChapDb = chapDb
                    });
                }
            }

            postDb.IsSynced = true;
            postDb.IdPostWpSynced = postWpSynced.Id;
            await _postDbRepository.ReplaceOneAsync(postDb);
        }

        public async Task JobSyncChap(ChapSyncMessage chapSyncMessage)
        {
            var request = chapSyncMessage.SiteConfigDb;
            var postWpId = chapSyncMessage.PostWpId;
            var chapDb = chapSyncMessage.ChapDb;

            // set init
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
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
            Console.WriteLine(string.Format("CHAP SYNCED: Id: {0}, Slug: {0}, Title: {1}", chapDb.Id, chapDb.Slug, chapDb.Titlte));

            chapDb.IsSynced = true;
            chapDb.IdPostWpSynced = chapWpSynced.Id;
            await _chapDbRepository.ReplaceOneAsync(chapDb);
        }
    }
}
