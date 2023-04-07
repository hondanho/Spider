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

namespace DotnetCrawler.Core {
    public class WordpressSyncCore : IWordpressSyncCore {
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
            IConfiguration configuration) {
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
            _categoryDbRepository = categoryDbRepository;
            _rabitMQProducer = rabitMQProducer;

            WordpressUriApi = configuration.GetValue<string>("Setting:WordpressUriApi");
            WordpressUserName = configuration.GetValue<string>("Setting:WordpressUserName");
            WordpressPassword = configuration.GetValue<string>("Setting:WordpressPassword");
        }

        public async Task SyncDataBySite(SiteConfigDb request) {
            // init
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _chapDbRepository.SetCollectionSave(request.BasicSetting.Document);
            _categoryDbRepository.SetCollectionSave(request.BasicSetting.Document);
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? WordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? WordpressUserName,
                request.BasicSetting.WordpressPassword ?? WordpressPassword
            );

            // sync category
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList();
            if(categoryDbs.Any()) {
                wpClient.Categories.QueryAsync(x => x.)


                foreach(var categoryDb in categoryDbs) { // fake

                    var categoryWp = await wpClient.Categories.CreateAsync(new Category() {
                        Slug = categoryDb.Slug,
                        Name = categoryDb.Titlte,
                        Taxonomy = "category"
                    });
                    Console.WriteLine(string.Format("CATEGORY SYNCED: Id: {0}, Slug: {0}, Title: {1}", categoryDb.Id, categoryDb.Slug, categoryDb.Titlte));

                    categoryDb.IsSynced = true;
                    categoryDb.IdPostWpSynced = categoryWp.Id;
                    await _categoryDbRepository.ReplaceOneAsync(categoryDb);
                }
            }

            // sync post
            var categoryWps = (await wpClient.Categories.GetAllAsync()).ToList();
            var postDbs = _postDbRepository.AsQueryable().ToList();
            if(postDbs.Any()) {
                var postDbsNotSync = postDbs.Where(pdb => !pdb.IsSynced).ToList();
                foreach(var postDb in postDbsNotSync) { // fake
                    var categoryWpIds = categoryWps.Where(cwp => postDb.CategorySlug == cwp.Slug)?.Select(cwp => cwp.Id).ToList();

                    _rabitMQProducer.SendMessage<PostSyncMessage>(QueueName.QueueSyncPost, new PostSyncMessage() {
                        PostDb = postDb,
                        CategoryIds = categoryWpIds,
                        SiteConfigDb = request
                    });
                }

                var postDbsSynced = postDbs.Where(pdb => pdb.IsSynced).ToList();
                foreach(var postDb in postDbsSynced) {
                    var isExistChapNotSync = _chapDbRepository.AsQueryable().Count(cdb => cdb.PostSlug == postDb.Slug && !cdb.IsSynced);
                    if(isExistChapNotSync > 0) {
                        var chapsNotSync = _chapDbRepository.FilterBy(cdb => !cdb.IsSynced && cdb.PostSlug == postDb.Slug).ToList();
                        if(chapsNotSync.Any()) {
                            foreach(var chapDb in chapsNotSync) { // fake
                                _rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage() {
                                    PostWpId = postDb.IdPostWpSynced,
                                    SiteConfigDb = request,
                                    ChapDb = chapDb
                                });
                            }

                            break; // fake
                        }
                    }
                }
            }
        }

        public async Task JobSyncPost(PostSyncMessage postSyncMessage) {
            var request = postSyncMessage.SiteConfigDb;
            var postDb = postSyncMessage.PostDb;
            var categoryWpIds = postSyncMessage.CategoryIds;

            // set init
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? WordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? WordpressUserName,
                request.BasicSetting.WordpressPassword ?? WordpressPassword
            );

            var postWp = new Post {
                Title = new Title(postDb.Titlte),
                Slug = postDb.Slug,
                Categories = categoryWpIds,
                Content = new Content(postDb.Description),
                Status = Status.Publish
                //Tags = ,
                //Author = ,
                //FeaturedMedia = 
            };

            var postWpSynced = await wpClient.Posts.CreateAsync(postWp);
            Console.WriteLine(string.Format("POST SYNCED: Id: {0}, Slug: {0}, Title: {1}", postDb.Id, postDb.Slug, postDb.Titlte));
            var chapsNotSync = _chapDbRepository.FilterBy(cdb => !cdb.IsSynced && cdb.PostSlug == postDb.Slug);
            if(chapsNotSync.Any()) {
                foreach(var chapDb in chapsNotSync) { // fake
                    _rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage() {
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

        public async Task JobSyncChap(ChapSyncMessage chapSyncMessage) {
            var request = chapSyncMessage.SiteConfigDb;
            var postWpId = chapSyncMessage.PostWpId;
            var chapDb = chapSyncMessage.ChapDb;

            // set init
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document);
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? WordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? WordpressUserName,
                request.BasicSetting.WordpressPassword ?? WordpressPassword
            );

            var newChapWp = new Chap {
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
