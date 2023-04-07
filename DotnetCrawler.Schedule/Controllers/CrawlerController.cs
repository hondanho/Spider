using DotnetCrawler.Api.Service;
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlerController : ControllerBase
    {
        private readonly ICrawlerService _crawlerService;
        private readonly ILogger<CrawlerController> _logger;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;
        private readonly IRabitMQProducer _rabitMQProducer;

        private int scheduleHourReCrawlerBig;
        private int scheduleHourReCrawlerSmall;

        public CrawlerController(
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            ICrawlerService crawlerService,
            IConfiguration configuration,
            IRabitMQProducer rabitMQProducer,
            ILogger<CrawlerController> logger)
        {
            _logger = logger;
            _crawlerService = crawlerService;
            _siteConfigDbRepository = siteConfigDbRepository;
            scheduleHourReCrawlerSmall = configuration.GetValue<int>("Setting:ScheduleHourReCrawlerSmall");
            scheduleHourReCrawlerBig = configuration.GetValue<int>("Setting:ScheduleHourReCrawlerBig");
            _rabitMQProducer = rabitMQProducer;
        }

        [HttpPost]
        [Route("recrawle-all")]
        public async Task ReCrawleAll()
        {
            await _crawlerService.ReCrawleAll();
        }

        [HttpPost]
        [Route("crawle-detail")]
        public async Task CrawleDetail(string siteId)
        {
           await _crawlerService.CrawlerBySiteId(siteId);
        }

        [HttpPost]
        [Route("update-post-chap-now")]
        public async Task UpdatePostChapNow() {
            await _crawlerService.UpdatePostChapAll();
        }

        [HttpPost]
        [Route("update-post-chap-schedule")]
        public async Task UpdatePostChapSchedule(int? hour)
        {
            hour = hour ?? scheduleHourReCrawlerSmall;
            await _crawlerService.UpdatePostChapScheduleAll(hour);
        }

        [HttpPost]
        [Route("clear-all")]
        public async Task ClearAllJobAndQueue() {
            await _crawlerService.ClearAllJobAndQueue();
        }
    }
}
