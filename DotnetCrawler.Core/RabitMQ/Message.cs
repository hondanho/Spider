using DotnetCrawler.Data.Entity;
using DotnetCrawler.Data.Entity.Setting;
using DotnetCrawler.Data.Model;
using System.Collections.Generic;
using WordPressPCL.Models;

namespace DotnetCrawler.Core.RabitMQ
{
    #region message queue crawler
    public class CategoryMessage
    {
        public string UrlCategoryCrawle { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
    }

    public class PostMessage
    {
        public LinkModel LinkPostCrawle { get; set; }
        public bool IsDuplicate { get; set; }
        public string CategorySlug { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public CategoryModel CategoryModel { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public int Index { get; set; }
        public bool IsNextCategory { get; set; }
        public string UrlCategoryNext { get; set; }
    }

    public class PostDetailMessage
    {
        public PostDb PostDb { get; set; }
        public string UrlPostCrawleNext { get; set; }
        public string UrlCategoryNext { get; set; }
        public CategoryDb CategoryDb { get; set; }
        public CategoryModel CategoryModel { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public bool IsNextCategory { get; set; }
    }

    public class ChapDetailMessage
    {
        public string PostSlug { get; set; }
        public string ChapUrl { get; set; }
        public SiteConfigDb SiteConfigDb { get; set; }
        public int Index { get; set; }
        public PostDb PostDb { get; set; }
        public bool IsNextPost { get; set; }
    }
    #endregion
}
