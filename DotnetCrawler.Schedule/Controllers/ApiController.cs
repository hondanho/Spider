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
        private readonly IMongoRepository<PostDb> _postDbRepository;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ICrawlerService crawlerService, ILogger<ApiController> logger, IMongoRepository<PostDb> postDbRepository) {
            _logger = logger;
            _postDbRepository = postDbRepository;
            _crawlerService = crawlerService;
        }

        [HttpGet]
        [Route("/")]
        public string Get() {
            return "hello";
        }

        [HttpPost]
        [Route("crawlersite")]
        public async Task<IActionResult> Welcome(string userName) {
            var jobId = BackgroundJob.Enqueue(() => _crawlerService.Crawler());
            return Ok($"Job Id {jobId} Completed. Welcome Mail Sent!");
        }

        [HttpPost]
        [Route("delayedWelcome")]
        public IActionResult DelayedWelcome(string userName)
        {
            var jobId = BackgroundJob.Schedule(() => Console.WriteLine($"Welcome to our application, {userName}"), TimeSpan.FromSeconds(10));
            return Ok($"Job Id {jobId} Completed. Delayed Welcome Mail Sent!");
        }

        [HttpPost]
        [Route("invoice")]
        public IActionResult Invoice(string userName)
        {
            RecurringJob.AddOrUpdate(() => Console.WriteLine($"Here is your invoice, {userName}"), Cron.Monthly);
            return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for {userName}!");
        }
    }
}
