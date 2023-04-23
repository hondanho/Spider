using DotnetCrawler.Data.Entity.Mongo.Log;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using System.Collections.Generic;
using WordPressPCL.Models;

namespace DotnetCrawler.Core.RabitMQ
{
    #region message queue crawler
    public class CategoryMessage
    {
        public string UrlCategoryCrawleNext { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class PostMessage
    {
        public LinkModel LinkPost { get; set; }
        public string StatusPost { get; set; }
        public bool IsDuplicate { get; set; }
        public string CategorySlug { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public int Index { get; set; }
    }

    public class PostDetailMessage
    {
        public PostDb PostDb { get; set; }
        public string UrlPostCrawleNext { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class ChapMessage
    {
        public string PostSlug { get; set; }
        public string ChapUrl { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public int Index { get; set; }
    }
    #endregion

    #region queue messasge sync
    public class PostSyncMessage
    {
        public List<int> CategoryIds { get; set; }
        public List<int> TacGiaIds { get; set; }
        public string MetaStatus { get; set; }
        public string MetaAlternativeName { get; set; }
        public string MetaSource { get; set; }
        public PostDb PostDb { get; set; }
        public PostLog PostLog { get; set; }
        public string CategorySlug { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public List<Category> Categories { get; set; }
    }

    public class ChapSyncMessage
    {
        public int PostWpId { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public ChapDb ChapDb { get; set; }
    }
    #endregion
}
