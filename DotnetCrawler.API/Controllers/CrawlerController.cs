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

        private int scheduleRecrawler;

        public CrawlerController(
            ICrawlerService crawlerService,
            IConfiguration configuration)
        {
            _crawlerService = crawlerService;
            scheduleRecrawler = configuration.GetValue<int>("Setting:ScheduleRecrawler");
        }

        [HttpPost]
        [Route("crawle")]
        public async Task Crawle()
        {
            await _crawlerService.Crawle();
        }

        [HttpPost]
        [Route("recrawle")]
        public async Task ReCrawleSchedule()
        {
            await _crawlerService.ReCrawleSchedule(scheduleRecrawler);
        }

        [HttpPost]
        [Route("force-recrawle")]
        public async Task ForceReCrawleSchedule() {
            await _crawlerService.ForceReCrawleSchedule();
        }
    }
}
