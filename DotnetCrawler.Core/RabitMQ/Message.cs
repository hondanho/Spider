using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;


namespace DotnetCrawler.Core.RabitMQ {
    public class CategoryMessage {
        public string UrlCategoryCrawleNext { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class PostMessage {
        public LinkModel LinkPost { get; set; }
        public bool IsDuplicate { get; set; }
        public string CategorySlug { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class PostDetailMessage {
        public PostDb PostDb { get; set; }
        public string UrlPostCrawleNext { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class ChapMessage {
        public string PostSlug { get; set; }
        public string ChapUrl { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }
}
