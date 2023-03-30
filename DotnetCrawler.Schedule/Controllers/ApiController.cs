using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase {

        private readonly ICrawlerService _crawlerService;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ICrawlerService crawlerService, ILogger<ApiController> logger) {
            _logger = logger;
            _crawlerService = crawlerService;
        }

        [HttpGet]
        [Route("/")]
        public string Get() {
            return "hello";
        }

        [HttpPost]
        [Route("run-process")]
        public async Task<IActionResult> RunProcessAsync() {
            var jobId = BackgroundJob.Enqueue(() => _crawlerService.Crawler());
            return Ok($"Job Id {jobId} Completed. Welcome Mail Sent!");
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
