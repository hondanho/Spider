using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SiteController : ControllerBase
    {

        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;
        private readonly ILogger<SiteController> _logger;

        public SiteController(IMongoRepository<SiteConfigDb> siteConfigDbRepository, ILogger<SiteController> logger)
        {
            _logger = logger;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        [HttpGet]
        public List<SiteConfigDb> GetAll()
        {
            return _siteConfigDbRepository.AsQueryable().ToList();
        }

        [HttpGet("{id}")]
        public async Task<SiteConfigDb> CreateSiteCrawler(string id)
        {
            return await _siteConfigDbRepository.FindByIdAsync(id);
        }

        [HttpPost]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task CreateSiteCrawler([FromBody] SiteConfigDb siteConfigDb)
        {
            await _siteConfigDbRepository.InsertOneAsync(siteConfigDb);
        }

        [HttpPut]
        [RequestSizeLimit(2147483648)] // e.g. 2 GB request limit
        public async Task<SiteConfigDb> UpdateSiteCrawler([FromBody] SiteConfigDb siteConfigDb)
        {
            if (siteConfigDb != null && !string.IsNullOrEmpty(siteConfigDb.IdString))
            {
                var siteConfig = await _siteConfigDbRepository.FindByIdAsync(siteConfigDb.IdString);
                if (siteConfig != null)
                {
                    await _siteConfigDbRepository.ReplaceOneAsync(siteConfigDb);
                }
            }

            return siteConfigDb;
        }

        [HttpDelete]
        public async Task DeleteSiteCrawler(string siteConfigId)
        {
            if (!string.IsNullOrEmpty(siteConfigId))
            {
                await _siteConfigDbRepository.DeleteByIdAsync(siteConfigId);
            }
        }
    }
}
