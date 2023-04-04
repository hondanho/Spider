using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Data.Setting {

    public class CategorySetting {

        public List<CategoryModel> CategoryModels;
        public string LinkPostSelector { get; set; }
        public string PagingSelector { get; set; }
    }

    public class CategoryModel
    {
        /// <summary>
        /// Set category name
        /// </summary>
        public string Titlte { get; set; }

        /// <summary>
        /// Url contains list post
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Slug to db, if not exist then create it
        /// </summary>
        public string Slug { get; set; }
    }
}
