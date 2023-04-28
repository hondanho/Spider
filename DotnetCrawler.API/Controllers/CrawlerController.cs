using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using DotnetCrawler.API.Service;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlerController : ControllerBase
    {
        private readonly ICrawlerService _crawlerService;

        private int scheduleMinuteCheckCrawlerAndRecrawler;

        public CrawlerController(
            ICrawlerService crawlerService,
            IConfiguration configuration)
        {
            _crawlerService = crawlerService;
            scheduleMinuteCheckCrawlerAndRecrawler = configuration.GetValue<int>("Setting:ScheduleMinuteCheckCrawlerAndRecrawler");
        }

        [HttpPost]
        [Route("crawle-all-schedule")]
        public async Task CrawleAllSchedule(int? minute)
        {
            minute = minute ?? scheduleMinuteCheckCrawlerAndRecrawler;
            await _crawlerService.CrawleAllSchedule(minute.Value);
        }

        [HttpPost]
        [Route("recrawle-all-schedule")]
        public async Task ReCrawleAllSchedule(int? minute)
        {
            minute = minute ?? scheduleMinuteCheckCrawlerAndRecrawler;
            await _crawlerService.ReCrawleAllSchedule(minute.Value);
        }
    }
}
