using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        private int scheduleHourReCrawlerBig;
        private int scheduleHourReCrawlerSmall;

        public CrawlerController(
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            ICrawlerService crawlerService,
            IConfiguration configuration,
            ILogger<CrawlerController> logger)
        {
            _logger = logger;
            _crawlerService = crawlerService;
            _siteConfigDbRepository = siteConfigDbRepository;
            scheduleHourReCrawlerSmall = configuration.GetValue<int>("Setting:ScheduleHourReCrawlerSmall");
            scheduleHourReCrawlerBig = configuration.GetValue<int>("Setting:ScheduleHourReCrawlerBig");
        }


        [HttpPost]
        [Route("crawler-all")]
        public async Task<IActionResult> CrawlerAll()
        {
            await _crawlerService.CrawlerAll();

            return Ok($"Crawler started");
        }

        [HttpPost]
        [Route("crawler-detail")]
        public async Task<IActionResult> CrawlerDetail(string siteId)
        {
           await _crawlerService.Crawler(siteId);

            return Ok($"Crawler started");
        }

        [HttpPost]
        [Route("recrawler-big-all")]
        public async Task<IActionResult> RecrawlerBig(int? hour)
        {
            hour = hour ?? scheduleHourReCrawlerBig;
            RecurringJob.AddOrUpdate(() => _crawlerService.ReCrawlerBig(), Cron.HourInterval(hour.Value));
            return Ok($"Recrawler Big Started");
        }

        [HttpPost]
        [Route("recrawler-small-all")]
        public async Task<IActionResult> RecrawlerSmall(int? hour)
        {
            hour = hour ?? scheduleHourReCrawlerSmall;
            RecurringJob.AddOrUpdate(() => _crawlerService.ReCrawlerSmall(), Cron.HourInterval(hour.Value));
            return Ok($"Recrawler Big Started");
        }


        [HttpPost]
        [Route("test")]
        public async Task<IActionResult> Test()
        {

            //client.Create(() => _crawlerService.TaskD(3, 5));
            //client.Create(() => _crawlerService.TaskD(4, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(5, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(6, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(7, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(8, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(9, 5));
            BackgroundJob.Enqueue(() => _crawlerService.TaskD(10, 5));

            return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for job 1");
        }
    }
}
