using DotnetCrawler.Data.Attributes;

namespace DotnetCrawler.Data.Models
{

    [BsonCollection("category")]
    public class CategoryDb : Document
    {
        public string Domain { get; set; }
        public string Titlte { get; set; }
        public string Url { get; set; }
        public string Slug { get; set; }
    }
}
