using DotnetCrawler.Api.Service;
using DotnetCrawler.API.Service.Wordpress;
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
    [Route("api/[controller]")]
    public class SyncDataController : ControllerBase
    {

        private readonly IWordpressService _wordpressService;
        private readonly ILogger<SyncDataController> _logger;

        private int scheduleHourSync;

        public SyncDataController(
            IWordpressService wordpressService,
            IConfiguration configuration,
            ILogger<SyncDataController> logger)
        {
            _logger = logger;
            _wordpressService = wordpressService;
            scheduleHourSync = configuration.GetValue<int>("Setting:ScheduleHourSync");
        }

        [HttpPost]
        [Route("sync-now")]
        public async Task SyncAllDataNow()
        {
            BackgroundJob.Enqueue(() => _wordpressService.SyncAllData());
        }

        [HttpPost]
        [Route("sync-schedule")]
        public async Task SyncAllData(int? hour)
        {
            hour = hour ?? scheduleHourSync;
            RecurringJob.AddOrUpdate(() => _wordpressService.SyncAllData(), Cron.HourInterval(hour.Value));
        }
    }
}
