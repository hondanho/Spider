using DotnetCrawler.Data.Attributes;
using MongoDB.Bson;

namespace DotnetCrawler.Data.Models
{

    [BsonCollection("post")]
    public class PostDb : Document
    {
        public string CategoryId { get; set; }
        public string Titlte { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public string Avatar { get; set; }
        public string Taxonomies { get; set; } // json data
        public string Metadata { get; set; } // json data
        public string UrlListChapPagingLatest { get; set; }

    }
}
