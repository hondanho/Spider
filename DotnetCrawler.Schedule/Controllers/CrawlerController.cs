using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using Hangfire;
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
    public class CrawlerController : ControllerBase
    {

        private readonly ICrawlerService _crawlerService;
        private readonly ILogger<CrawlerController> _logger;
        private readonly IMongoRepository<SiteConfigDb> _siteConfigDbRepository;

        public CrawlerController(IMongoRepository<SiteConfigDb> siteConfigDbRepository, ICrawlerService crawlerService, ILogger<CrawlerController> logger)
        {
            _logger = logger;
            _crawlerService = crawlerService;
            _siteConfigDbRepository = siteConfigDbRepository;
        }

        [HttpGet]
        [Route("/")]
        public string Get()
        {
            return "hello";
        }

        [HttpPost()]
        [Route("run-process")]
        public async Task<IActionResult> RunProcessAsync(string siteId)
        {
            if (string.IsNullOrEmpty(siteId)) return NotFound();
            var siteConfig = await _siteConfigDbRepository.FindByIdAsync(siteId);
            if (siteConfig == null) return NotFound();
            var jobId = BackgroundJob.Enqueue(() => _crawlerService.Crawler(new SiteConfigDb()
            {
                BasicSetting = siteConfig.BasicSetting,
                CategorySetting = siteConfig.CategorySetting,
                PostSetting = siteConfig.PostSetting,
                ChapSetting = siteConfig.ChapSetting
            }));
            return Ok($"Job Id {jobId} Completed");
        }

        [HttpPost]
        [Route("delayed-process")]
        public async Task<IActionResult> DelayedProcessAsync()
        {
            var jobId = BackgroundJob.Schedule(() => Console.WriteLine($"Welcome to our application, "), TimeSpan.FromSeconds(10));
            return Ok($"Job Id {jobId} Completed. Delayed Welcome Mail Sent!");
        }

        [HttpPost]
        [Route("schedule-process")]
        public async Task<IActionResult> ScheduleProcessAsync()
        {
            RecurringJob.AddOrUpdate(() => Console.WriteLine($"Here is your invoice"), Cron.MinuteInterval(1));
            return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for !");
        }
    }
}
