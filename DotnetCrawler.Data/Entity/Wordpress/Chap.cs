using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WordPressPCL.Models;

namespace DotnetCrawler.Data.Entity.Wordpress
{
    public class Chap : Post
    {
        [JsonProperty("parent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Parent { get; set; }
    }
}
