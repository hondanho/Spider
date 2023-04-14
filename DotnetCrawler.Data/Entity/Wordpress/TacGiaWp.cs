using Newtonsoft.Json;
using WordPressPCL.Models;

namespace DotnetCrawler.Data.Entity.Wordpress
{
    public class TacGiaWp : Term
    {
        [JsonProperty("term_id")]
        public int TermID { get; set; }
    }
}