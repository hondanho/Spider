using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WordPressPCL.Models;

namespace DotnetCrawler.Data.Entity.Wordpress
{
    public class PostWp : Post
    {
        //public List<string> Author { get; set; }
        //public List<string> AlternativeName { get; set; }
        //public List<string> Genres { get; set; }
        //public string Source { get; set; }
        //public string Status { get; set; }
        //public List<string> Tags { get; set; }

        [JsonProperty("tac-gia", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<int> TacGia { get; set; }


    }
}
