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
