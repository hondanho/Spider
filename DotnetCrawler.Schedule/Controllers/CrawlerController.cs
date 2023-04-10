using DotnetCrawler.Api.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlerController : ControllerBase
    {
        private readonly ICrawlerService _crawlerService;

        private int scheduleHourReCrawler;
        private int scheduleHourUpdatePostChap;

        public CrawlerController(
            ICrawlerService crawlerService,
            IConfiguration configuration)
        {
            _crawlerService = crawlerService;
            scheduleHourUpdatePostChap = configuration.GetValue<int>("Setting:ScheduleHourUpdatePostChap");
            scheduleHourReCrawler = configuration.GetValue<int>("Setting:ScheduleHourReCrawler");
        }

        [HttpPost]
        [Route("recrawle-all")]
        public async Task ReCrawleAll()
        {
            await _crawlerService.ReCrawleAll();
        }

        [HttpPost]
        [Route("recrawle-all-schedule")]
        public async Task ReCrawleAllSchedule(int? hour)
        {
            hour = hour ?? scheduleHourReCrawler;
            await _crawlerService.ReCrawleAllSchedule(hour.Value);
        }

        [HttpPost]
        [Route("crawle-detail")]
        public async Task CrawleDetail(string siteId)
        {
            await _crawlerService.CrawlerBySiteId(siteId);
        }

        [HttpPost]
        [Route("update-post-chap-now")]
        public async Task UpdatePostChapNow()
        {
            await _crawlerService.UpdatePostChapAll();
        }

        [HttpPost]
        [Route("update-post-chap-schedule")]
        public async Task UpdatePostChapSchedule(int? hour)
        {
            hour = hour ?? scheduleHourUpdatePostChap;
            await _crawlerService.UpdatePostChapScheduleAll(hour.Value);
        }

        [HttpPost]
        [Route("clear-all")]
        public async Task ClearAllJobAndQueue()
        {
            await _crawlerService.ClearAllJobAndQueue();
        }
    }
}
