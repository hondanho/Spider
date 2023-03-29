using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Schedule.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger) {
            _logger = logger;
        }

        [HttpGet]
        public string Get() {
            return "hello";
        }

        [HttpPost]
        [Route("welcome")]
        public IActionResult Welcome(string userName) {
            var jobId = BackgroundJob.Enqueue(() => SendWelcomeMail(userName));
            return Ok($"Job Id {jobId} Completed. Welcome Mail Sent!");
        }

        public void SendWelcomeMail(string userName) {
            //Logic to Mail the user
            Console.WriteLine($"Welcome to our application, {userName}");
        }

        [HttpPost]
        [Route("delayedWelcome")]
        public IActionResult DelayedWelcome(string userName) {
            var jobId = BackgroundJob.Schedule(() => SendDelayedWelcomeMail(userName), TimeSpan.FromSeconds(10));
            return Ok($"Job Id {jobId} Completed. Delayed Welcome Mail Sent!");
        }

        public void SendDelayedWelcomeMail(string userName) {
            //Logic to Mail the user
            Console.WriteLine($"Welcome to our application, {userName}");
        }

        [HttpPost]
        [Route("invoice")]
        public IActionResult Invoice(string userName) {
            RecurringJob.AddOrUpdate(() => SendInvoiceMail(userName), Cron.Monthly);
            return Ok($"Recurring Job Scheduled. Invoice will be mailed Monthly for {userName}!");
        }

        public void SendInvoiceMail(string userName) {
            //Logic to Mail the user
            Console.WriteLine($"Here is your invoice, {userName}");
        }
    }
}
