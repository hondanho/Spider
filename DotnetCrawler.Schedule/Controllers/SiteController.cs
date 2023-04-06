using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System;

namespace DotnetCrawler.Api.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SiteController : ControllerBase {
        private readonly ICrawlerService _crawlerService;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly ILogger<SiteController> _logger;

        public SiteController(
            ILogger<SiteController> logger,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            ICrawlerService crawlerService) {
            _logger = logger;
            _siteConfigDbRepository = siteConfigDbRepository;
            _crawlerService = crawlerService;
            _categoryDbRepository = categoryDbRepository;
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
        }

        [HttpGet("check")]
        public CheckerDataModel GetDuplicateByPostSlug() {
            var result = new CheckerDataModel();

            var duplicateCategory = _categoryDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p=> p.Titlte)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.CategoryDuplicate = new Duplicate {
                DuplicateRecords = duplicateCategory,
                Count = duplicateCategory.Count()
            };

            var duplicatePost = _postDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p => p.CategorySlug)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.PostDuplicate = new Duplicate {
                DuplicateRecords = duplicatePost,
                Count = duplicatePost.Count
            };

            var duplicateChap = _chapDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p => p.PostSlug)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.ChapDuplicate = new Duplicate {
                DuplicateRecords = duplicateChap,
                Count = duplicateChap.Count()
            };

            return result;
        }


        [HttpGet("count")]
        public async Task<List<CategoryInfo>> GetSiteInfos(int page) {
            var result = new List<CategoryInfo>();
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList();
            if(categoryDbs.Any()) {
                foreach(var category in categoryDbs) {
                    var postInfos = new List<PostInfo>();
                    if(page < 1)
                        page = 1;
                    var postDbs = _postDbRepository.AsQueryable().Skip((page - 1) * 50).Take(50);
                    foreach(var postDb in postDbs) {
                        var countChap = _chapDbRepository.AsQueryable().Count(chap => chap.PostSlug == postDb.Slug);

                        postInfos.Add(new PostInfo {
                            ChapCount = countChap,
                            PostDb = postDb
                        });
                    }

                    result.Add(new CategoryInfo {
                        CategoryDb = category,
                        PostCount = postInfos.Count,
                        PostInfos = postInfos
                    });
                }
            }

            return result;
        }

        [HttpGet("{id}")]
        public async Task<SiteConfigDb> GetDetailSite(string id) {
            return await _siteConfigDbRepository.FindByIdAsync(id);
        }

        [HttpPost]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task CreateSite([FromBody] SiteConfigDb siteConfigDb) {
            siteConfigDb.SystemStatus = new SystemStatus() {
                Status = StatusCrawler.DEFAULT
            };
            await _siteConfigDbRepository.InsertOneAsync(siteConfigDb);
            if(siteConfigDb.BasicSetting.IsThuThap) {
                await _crawlerService.Crawler(siteConfigDb.Id);
            }
        }

        [HttpPut]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task<SiteConfigDb> UpdateSite([FromBody] SiteConfigDb siteConfig) {
            if(siteConfig != null && !string.IsNullOrEmpty(siteConfig.Id)) {
                var siteConfigDb = await _siteConfigDbRepository.FindByIdAsync(siteConfig.Id);
                if(siteConfigDb != null) {
                    await _siteConfigDbRepository.ReplaceOneAsync(siteConfig);
                }

                if(siteConfigDb.BasicSetting.IsThuThap && !siteConfig.BasicSetting.IsThuThap && !string.IsNullOrEmpty(siteConfigDb.SystemStatus.JobId)) // đang thu thập -> ko thu thập
                {
                    BackgroundJob.Delete(siteConfigDb.SystemStatus.JobId);
                } else if(!siteConfigDb.BasicSetting.IsThuThap && siteConfig.BasicSetting.IsThuThap) // ko thu thập -> thu thập
                  {
                    await _crawlerService.Crawler(siteConfigDb.Id);
                }
            }

            return siteConfig;
        }

        [HttpDelete]
        public async Task DeleteSite(string siteConfigId) {
            if(!string.IsNullOrEmpty(siteConfigId)) {
                var siteConfigDb = await _siteConfigDbRepository.FindByIdAsync(siteConfigId);
                if(siteConfigDb != null) {
                    await _siteConfigDbRepository.DeleteByIdAsync(siteConfigId);
                }

                if(!string.IsNullOrEmpty(siteConfigDb.SystemStatus.JobId)) {
                    BackgroundJob.Delete(siteConfigDb.SystemStatus.JobId);
                }
            }
        }
    }
}
