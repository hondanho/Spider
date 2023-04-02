using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SiteController : ControllerBase
    {
        private readonly ICrawlerService _crawlerService;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;
        private readonly ILogger<SiteController> _logger;

        public SiteController(IMongoRepository<SiteConfigDb> siteConfigDbRepository, ILogger<SiteController> logger,
            ICrawlerService crawlerService)
        {
            _logger = logger;
            _siteConfigDbRepository = siteConfigDbRepository;
            _crawlerService = crawlerService;
        }

        [HttpGet]
        public List<SiteConfigDb> GetAll()
        {
            return _siteConfigDbRepository.AsQueryable().ToList();
        }

        [HttpGet("{id}")]
        public async Task<SiteConfigDb> GetDetailSite(string id)
        {
            return await _siteConfigDbRepository.FindByIdAsync(id);
        }

        [HttpPost]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task CreateSite([FromBody] SiteConfigDb siteConfigDb)
        {
            siteConfigDb.SystemStatus.Status = Data.Setting.StatusCrawler.DEFAULT;
            await _siteConfigDbRepository.InsertOneAsync(siteConfigDb);
            if (siteConfigDb.BasicSetting.IsThuThap)
            {
                await _crawlerService.Crawler(siteConfigDb.IdString);
            }
        }

        [HttpPut]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task<SiteConfigDb> UpdateSite([FromBody] SiteConfigDb siteConfig)
        {
            if (siteConfig != null && !string.IsNullOrEmpty(siteConfig.IdString))
            {
                var siteConfigDb = await _siteConfigDbRepository.FindByIdAsync(siteConfig.IdString);
                if (siteConfigDb != null)
                {
                    await _siteConfigDbRepository.ReplaceOneAsync(siteConfig);
                }

                if (siteConfigDb.BasicSetting.IsThuThap && !siteConfig.BasicSetting.IsThuThap && !string.IsNullOrEmpty(siteConfigDb.SystemStatus.JobId)) // đang thu thập -> ko thu thập
                {
                    BackgroundJob.Delete(siteConfigDb.SystemStatus.JobId);
                }
                else if (!siteConfigDb.BasicSetting.IsThuThap && siteConfig.BasicSetting.IsThuThap) // ko thu thập -> thu thập
                {
                    await _crawlerService.Crawler(siteConfigDb.IdString);
                }
            }

            return siteConfig;
        }

        [HttpDelete]
        public async Task DeleteSite(string siteConfigId)
        {
            if (!string.IsNullOrEmpty(siteConfigId))
            {
                var siteConfigDb = await _siteConfigDbRepository.FindByIdAsync(siteConfigId);
                if (siteConfigDb != null)
                {
                    await _siteConfigDbRepository.DeleteByIdAsync(siteConfigId);
                }

                if (!string.IsNullOrEmpty(siteConfigDb.SystemStatus.JobId))
                {
                    BackgroundJob.Delete(siteConfigDb.SystemStatus.JobId);
                }
            }
        }
    }
}
