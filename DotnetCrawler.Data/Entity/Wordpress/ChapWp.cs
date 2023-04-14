using Newtonsoft.Json;
using WordPressPCL.Models;

namespace DotnetCrawler.Data.Entity.Wordpress
{
    public class ChapWp : Post
    {
        [JsonProperty("parent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Parent { get; set; }
    }
}
