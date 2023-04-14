using DotnetCrawler.Data.Constants;
using Newtonsoft.Json;
using WordPressPCL.Models;

namespace DotnetCrawler.Data.Entity.Wordpress
{
    public class MetaWp
    {
        [JsonProperty(MetaFieldPost.Source, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(MetaFieldPost.Status, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Status { get; set; }

        [JsonProperty(MetaFieldPost.AlternativeName, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AlternativeName { get; set; }
    }
}
