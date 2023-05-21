using DotnetCrawler.API.Service;
using DotnetCrawler.Base.Extension;
using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            ICrawlerService crawlerService)
        {
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
            if (siteConfigDb != null && siteConfigDb.CategorySetting != null &&
            siteConfigDb.CategorySetting.CategoryModels != null &&
            siteConfigDb.CategorySetting.CategoryModels.Any())
            {
                var newCategoryModels = new List<CategoryModel>();
                var listCategoryDbs = _categoryDbRepository.AsQueryable().ToList();
                foreach (var categoryModel in siteConfigDb.CategorySetting.CategoryModels)
                {
                    if (!string.IsNullOrEmpty(categoryModel.Slug) &&
                        !string.IsNullOrEmpty(categoryModel.Titlte) &&
                        !string.IsNullOrEmpty(categoryModel.Url))
                    {
                        categoryModel.Slug = Helper.CleanSlug(categoryModel.Slug);
                        if ((listCategoryDbs.Any() && !listCategoryDbs.Any(item => item.Slug == categoryModel.Slug)) ||
                            !listCategoryDbs.Any())
                        {
                            newCategoryModels.Add(categoryModel);
                        }
                    }
                }
                

                _siteConfigDbRepository.SetCollectionSave(siteConfigDb.BasicSetting.Document);
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
