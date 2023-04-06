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
    [Route("[controller]")]
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
        public async Task<IActionResult> ReCrawleAll()
        {
            await _crawlerService.ReCrawleAll();

            return Ok($"ReCrawlerAll started");
        }

        [HttpPost]
        [Route("crawle-detail")]
        public async Task<IActionResult> CrawleDetail(string siteId)
        {
           await _crawlerService.Crawler(siteId);

            return Ok($"CrawleDetail Started");
        }

        [HttpPost]
        [Route("update-post-chap-now")]
        public async Task<IActionResult> UpdatePostChapNow() {
            await _crawlerService.UpdatePostChap();
            return Ok($"UpdatePostChapNow Started");
        }

        [HttpPost]
        [Route("update-post-chap-schedule")]
        public async Task<IActionResult> UpdatePostChapSchedule(int? hour)
        {
            hour = hour ?? scheduleHourReCrawlerSmall;
            RecurringJob.AddOrUpdate(() => _crawlerService.UpdatePostChap(), Cron.HourInterval(hour.Value));
            return Ok($"UpdatePostChap Started");
        }
    }
}
