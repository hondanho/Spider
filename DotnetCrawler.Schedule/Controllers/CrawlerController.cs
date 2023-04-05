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


        //[HttpPost]
        //[Route("test")]
        //public async Task<IActionResult> Test(int number)
        //{
        //    BackgroundJob.Enqueue(() => _crawlerService.TaskD(number, 5));
        //    //var jobId1 = BackgroundJob.Enqueue(() => _crawlerService.TaskD(1, 5));
        //    //var jobId2 = BackgroundJob.ContinueJobWith(jobId1 , () => _crawlerService.TaskD(2, 5));
        //    //var jobId3 =  BackgroundJob.ContinueJobWith(jobId2, () => _crawlerService.TaskD(3, 5));

        //    return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for job 1");
        //}

        //[HttpPost]
        //[Route("test2")]
        //public async Task<IActionResult> Test2(string thuan) {
        //    _rabitMQProducer.SendChapMessage<string>("hello t la thuan day" + thuan);
        //    // create a new instance of BackgroundJobClient
        //    var client = new BackgroundJobClient();

        //    // get all jobs

        //    return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for job 2");
        //}
    }
}
