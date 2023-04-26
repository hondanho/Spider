using DotnetCrawler.Base.Extension;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase {

        public TestController() {
        }

        [HttpGet("text")]
        public async Task<List<string>> GetTextsFromSelector(string cssSelector, string url) {
            return await Helper.GetTextsFromSelector(cssSelector, url);
        }

        [HttpGet("image")]
        public async Task<string> GetUrlImageFromSelector(string cssSelector, string url)
        {
            return await Helper.GetUrlImageFromSelector(cssSelector, url);
        }
    }
}
