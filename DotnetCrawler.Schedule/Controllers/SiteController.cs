using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class SiteController : ControllerBase {
        private readonly ICrawlerService _crawlerService;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        public SiteController(
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            ICrawlerService crawlerService) {
            _siteConfigDbRepository = siteConfigDbRepository;
            _crawlerService = crawlerService;
        }

        [HttpGet("{id}")]
        public async Task<SiteConfigDb> GetDetailSite(string id) {
            return await _siteConfigDbRepository.FindByIdAsync(id);
        }

        [HttpPost]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task CreateSite([FromBody] SiteConfigDb siteConfigDb) {
            await _siteConfigDbRepository.InsertOneAsync(siteConfigDb);
            if(siteConfigDb.BasicSetting.IsThuThap) {
                await _crawlerService.CrawlerBySiteId(siteConfigDb.Id);
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

                if(!siteConfigDb.BasicSetting.IsThuThap && siteConfig.BasicSetting.IsThuThap) // ko thu thập -> thu thập -> crawle lai
                  {
                    await _crawlerService.CrawlerBySiteId(siteConfigDb.Id);
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
            }
        }
    }
}
