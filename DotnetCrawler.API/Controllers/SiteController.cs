using DotnetCrawler.API.Service;
using DotnetCrawler.Base.Extension;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL.Models;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiteController : ControllerBase
    {
        private readonly ICrawlerService _crawlerService;
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        public SiteController(
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            IMongoRepository<CategoryDb> categoryDbRepository,
            ICrawlerService crawlerService) {
            _categoryDbRepository = categoryDbRepository;
            _siteConfigDbRepository = siteConfigDbRepository;
            _crawlerService = crawlerService;
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
            var listCategoryDbs = _categoryDbRepository.AsQueryable().ToList();
            if (siteConfigDb != null && siteConfigDb.CategorySetting != null &&
                siteConfigDb.CategorySetting.CategoryModels != null &&
                siteConfigDb.CategorySetting.CategoryModels.Any()) {
                siteConfigDb.CategorySetting.CategoryModels = siteConfigDb.CategorySetting.CategoryModels.Where(item =>
                    !string.IsNullOrEmpty(item.Slug) &&
                    !string.IsNullOrEmpty(item.Titlte) &&
                    !string.IsNullOrEmpty(item.Url) &&
                    listCategoryDbs.Any(ctgDbs => ctgDbs.Slug == item.Slug && ctgDbs.Url == item.Url)
                ).ToList();
                await _siteConfigDbRepository.InsertOneAsync(siteConfigDb);
            }
        }

        [HttpPut]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task<SiteConfigDb> UpdateSite([FromBody] SiteConfigDb siteConfig)
        {
            if (siteConfig != null && !string.IsNullOrEmpty(siteConfig.Id))
            {
                var siteConfigDb = await _siteConfigDbRepository.FindByIdAsync(siteConfig.Id);
                if (siteConfigDb != null)
                {
                    await _siteConfigDbRepository.ReplaceOneAsync(siteConfig);
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
            }
        }
    }
}
