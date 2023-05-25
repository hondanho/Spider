using DotnetCrawler.Data.Attributes;
using System.Collections.Generic;

namespace DotnetCrawler.Data.Entity
{

    [BsonCollection("post")]
    public class PostDb : Document
    {
        public string Slug { get; set; }
        public string Url { get; set; }
        public string Avatar { get; set; }
        public string CategorySlug { get; set; }
        public string Status { get; set; }
        public string Titlte { get; set; }
        public string Description { get; set; }
        public Dictionary<string, List<string>> Metadatas { get; set; }
        public string UrlPostPagingCrawleLatest { get; set; }
        public int Index { get; set; }
        public bool IsCrawleDone { get; set; }
    }
}
