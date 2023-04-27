using DotnetCrawler.Data.Attributes;

namespace DotnetCrawler.Data.Entity
{

    [BsonCollection("category")]
    public class CategoryDb : Document
    {
        public string Titlte { get; set; }
        public string Slug { get; set; }
        public string UrlCategoryPagingNext { get; set; }
        public string UrlCategoryPagingLatest { get; set; }
    }
}
