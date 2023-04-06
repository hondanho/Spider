using DotnetCrawler.Data.Attributes;

namespace DotnetCrawler.Data.Models
{

    [BsonCollection("category")]
    public class CategoryDb : Document
    {
        public string Titlte { get; set; }
        public string Slug { get; set; }
        public string UrlCrawlePostPagingLatest { get; set; }
    }
}
