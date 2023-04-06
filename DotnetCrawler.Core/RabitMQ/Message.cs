using DotnetCrawler.Data.Entity.Wordpress;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using System.Collections.Generic;
using WordPressPCL.Models;

namespace DotnetCrawler.Core.RabitMQ {

    /// <summary>
    /// message queue crawle category
    /// </summary>
    public class CategoryMessage {
        public string UrlCategoryCrawleNext { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    /// <summary>
    /// message queue crawle post
    /// </summary>
    public class PostMessage {
        public LinkModel LinkPost { get; set; }
        public bool IsDuplicate { get; set; }
        public string CategorySlug { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    /// <summary>
    /// messasge queue crawle post detail
    /// </summary>
    public class PostDetailMessage {
        public PostDb PostDb { get; set; }
        public string UrlPostCrawleNext { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    /// <summary>
    /// message queue crawle chap
    /// </summary>
    public class ChapMessage {
        public string PostSlug { get; set; }
        public string ChapUrl { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    /// <summary>
    /// message queue sync post to another db
    /// </summary>
    public class PostSyncMessage
    {
        public List<PostDb> PostDbs { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public List<Category> Categories { get; set; }
    }

    /// <summary>
    /// message queue sync chap to another db
    /// </summary>
    public class ChapSyncMessage
    {
        public List<ChapDb> ChapDbs { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }

    }
}
