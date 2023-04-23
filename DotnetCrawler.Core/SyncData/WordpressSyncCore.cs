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
using MongoDB.Driver.Linq;

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
            _categoryDbRepository.SetCollectionSave(request.BasicSetting.Document ?? databaseName);
            _postLogRepository.SetCollectionSave(databaseLog);

            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            if (request.CategorySetting.CategoryModels != null && request.CategorySetting.CategoryModels.Any())
            {
                var categoryWps = await wpClient.Categories.GetAllAsync();
                var tacGiaWps = await wpClient.CustomRequest.GetAsync<List<TacGiaWp>>($"/wp-json/wp/v2/all-terms?term=tac-gia");

                var postDbs = _postDbRepository.FilterBy(pdb =>
                                    request.CategorySetting.CategoryModels.Any(csm => csm.Slug == pdb.CategorySlug)
                                ).ToList();
                if (!postDbs.Any()) return;

                foreach (var postDb in postDbs.Where(pdb => pdb.Metadatas != null))
                {
                    // sync genre
                    var metadataGenres = postDb.Metadatas.FirstOrDefault(metadata =>
                        metadata.Key == MetaFieldPost.Genre &&
                        metadata.Value != null &&
                        metadata.Value.Any()
                    );
                    foreach (var categoryTxt in metadataGenres.Value)
                    {
                        var slug = categoryTxt.Trim().Replace(" ", "-").ToLower();
                        if (!categoryWps.Any(cwp => cwp.Slug == slug))
                        {
                            var categoryWp = await wpClient.Categories.CreateAsync(new Category()
                            {
                                Slug = slug,
                                Name = categoryTxt.Trim(),
                                Taxonomy = "category"
                            });
                            Console.WriteLine(string.Format("CATEGORY SYNCED: Id: {0}, Slug: {0}, Title: {1}", categoryWp.Id, categoryWp.Slug, categoryWp.Name));
                        }
                    }

                    // sync tac-gia
                    var metadataTacGia = postDb.Metadatas.FirstOrDefault(metadata =>
                        metadata.Key == MetaFieldPost.TacGia &&
                        metadata.Value != null &&
                        metadata.Value.Any()
                    );
                    foreach (var tacgiaTxt in metadataTacGia.Value)
                    {
                        var slug = tacgiaTxt.Trim().Replace(" ", "-").ToLower();
                        if (!tacGiaWps.Any(twp => twp.Name == tacgiaTxt || twp.Slug == slug))
                        {
                            try
                            {
                                var tacgia = await wpClient.CustomRequest.CreateAsync<TacGiaWp, TacGiaWp>($"/wp-json/wp/v2/tac-gia", new TacGiaWp
                                {
                                    Slug = slug,
                                    Name = tacgiaTxt.Trim(),
                                    Taxonomy = "tac-gia"
                                });
                                Console.WriteLine(string.Format("TAC GIA SYNCED: Id: {0}, Slug: {0}, Title: {1}", tacgia.Id, tacgia.Slug, tacgia.Name));
                            } catch (Exception ex) { 
                                Console.WriteLine(ex?.Message);
                            }
                        }
                    }
                }

                tacGiaWps = await wpClient.CustomRequest.GetAsync<List<TacGiaWp>>($"/wp-json/wp/v2/all-terms?term=tac-gia");
                categoryWps = await wpClient.Categories.GetAllAsync();
                foreach (var categoryDb in request.CategorySetting.CategoryModels)
                {
                    // sync post
                    var postLogs = _postLogRepository.FilterBy(pl => pl.CategorySlug == categoryDb.Slug).ToList();
                    foreach (var postDb in postDbs)
                    {
                        var categoryIds = new List<int>();
                        var tacgiaIds = new List<int>();

                        var metadataGenres = postDb.Metadatas.FirstOrDefault(metadata =>
                                            metadata.Key == MetaFieldPost.Genre &&
                                            metadata.Value != null &&
                                            metadata.Value.Any()
                                        );
                        foreach (var categoryTxt in metadataGenres.Value.Where(x => !string.IsNullOrEmpty(x)))
                        {
                            var slug = categoryTxt.Trim().Replace(" ", "-").ToLower();
                            var categoryWp = categoryWps.FirstOrDefault(cwp => cwp.Slug == slug);
                            if (categoryWp != null)
                            {
                                categoryIds.Add(categoryWp.Id);
                            }
                        }

                        var metadataTacGia = postDb.Metadatas.FirstOrDefault(metadata =>
                            metadata.Key == MetaFieldPost.TacGia &&
                            metadata.Value != null &&
                            metadata.Value.Any()
                        );
                        foreach (var tacgiaTxt in metadataTacGia.Value)
                        {
                            var slug = tacgiaTxt.Trim().Replace(" ", "-").ToLower();
                            var tacgiaWp = tacGiaWps.FirstOrDefault(cwp => cwp.Slug == slug);
                            if (tacgiaWp != null)
                            {
                                tacgiaIds.Add(tacgiaWp.TermID);
                            }
                        }

                        var metadataSource = postDb.Metadatas.FirstOrDefault(metadata =>
                            metadata.Key == MetaFieldPost.Source &&
                            metadata.Value != null &&
                            metadata.Value.Any()
                        );
                        var metadataStatus = postDb.Metadatas.FirstOrDefault(metadata =>
                            metadata.Key == MetaFieldPost.Status &&
                            metadata.Value != null &&
                            metadata.Value.Any()
                        );
                        var metadataAlternativeName = postDb.Metadatas.FirstOrDefault(metadata =>
                            metadata.Key == MetaFieldPost.AlternativeName &&
                            metadata.Value != null &&
                            metadata.Value.Any()
                        );
                        var postLog = postLogs.FirstOrDefault(pl => pl.Slug == postDb.Slug && pl.PostId > 0);
                        _rabitMQProducer.SendMessage<PostSyncMessage>(QueueName.QueueSyncPost, new PostSyncMessage()
                        {
                            PostLog = postLog,
                            PostDb = postDb,
                            SiteConfigDb = request,
                            CategorySlug = categoryDb.Slug,

                            TacGiaIds = tacgiaIds,
                            CategoryIds = categoryIds,
                            MetaStatus = metadataStatus.Value.FirstOrDefault(),
                            MetaAlternativeName = metadataAlternativeName.Value.Count > 1 ? metadataAlternativeName.Value[1] : metadataAlternativeName.Value.FirstOrDefault(),
                            MetaSource= metadataSource.Value.FirstOrDefault()
                        });
                    }
                }
            }
        }

        public async Task JobSyncPost(PostSyncMessage postSyncMessage)
        {
            // set init
            var request = postSyncMessage.SiteConfigDb;
            var postDb = postSyncMessage.PostDb;
            var postLog = postSyncMessage.PostLog;
            _postDbRepository.SetCollectionSave(request.BasicSetting.Document ?? databaseName);
            _chapDbRepository.SetCollectionSave(request.BasicSetting.Document ?? databaseName);
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
                    }
                    catch (Exception ex)
                    {
                    }
                }

                var postWp = await wpClient.Posts.CreateAsync(new PostWp
                {
                    Title = new Title(postDb.Titlte),
                    Slug = postDb.Slug,
                    Categories = postSyncMessage.CategoryIds,
                    Content = new Content(postDb.Description),
                    Status = Status.Publish,
                    //Tags = ,
                    FeaturedMedia = featureId,
                    TacGia = postSyncMessage.TacGiaIds,
                    Meta = new MetaWp
                    {
                        Source = postSyncMessage.MetaSource,
                        Status = postSyncMessage.MetaStatus,
                        AlternativeName = postSyncMessage.MetaAlternativeName
                    }
                });

                postLog = new PostLog
                {
                    CategorySlug = postSyncMessage.CategorySlug,
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
                foreach (var chapDb in chapsDbsNotSync.OrderBy(cdb => cdb.Index).ToList())
                {
                    var newChapWp = new ChapWp
                    {
                        Content = new Content(chapDb.Content),
                        Parent = postLog.PostId,
                        Slug = chapDb.Slug,
                        Type = "chap",
                        Title = new Title(chapDb.Titlte),
                        Status = Status.Publish
                    };
                    var chapWpSynced = await wpClient.CustomRequest.CreateAsync<ChapWp, ChapWp>("/wp-json/wp/v2/chap", newChapWp);
                    var newChapLog = new ChapLog
                    {
                        Slug = chapDb.Slug,
                        PostSlug = chapDb.PostSlug,
                        ChapId = chapWpSynced.Id
                    };
                    await _chapLogRepository.InsertOneAsync(newChapLog);
                    Console.WriteLine(string.Format("CHAP SYNCED: Id: {0}, Slug: {0}, Title: {1}", chapDb.Id, chapDb.Slug, chapDb.Titlte));
                    //_rabitMQProducer.SendMessage<ChapSyncMessage>(QueueName.QueueSyncChap, new ChapSyncMessage()
                    //{
                    //    PostWpId = postLog.PostId,
                    //    SiteConfigDb = request,
                    //    ChapDb = chapDb
                    //});
                }
            }
        }

        public async Task JobSyncChap(ChapSyncMessage chapSyncMessage)
        {
            var request = chapSyncMessage.SiteConfigDb;
            var chapDb = chapSyncMessage.ChapDb;
            _chapLogRepository.SetCollectionSave(databaseLog);

            // set init
            var wpClient = new WordPressClient(request.BasicSetting.WordpressUriApi ?? wordpressUriApi);
            wpClient.Auth.UseBasicAuth(
                request.BasicSetting.WordpressUserName ?? wordpressUserName,
                request.BasicSetting.WordpressPassword ?? wordpressPassword
            );

            var newChapWp = new ChapWp
            {
                Content = new Content(chapDb.Content),
                Parent = chapSyncMessage.PostWpId,
                Slug = chapDb.Slug,
                Type = "chap",
                Title = new Title(chapDb.Titlte),
                Status = Status.Publish
            };
            var chapWpSynced = await wpClient.CustomRequest.CreateAsync<ChapWp, ChapWp>("/wp-json/wp/v2/chap", newChapWp);
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
