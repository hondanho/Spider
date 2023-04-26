using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Data.Entity.Setting
{

    public class CategorySetting
    {

        public List<CategoryModel> CategoryModels { get; set; }
        public string LinkPostSelector { get; set; }
        public string PagingSelector { get; set; }
        /// <summary>
        /// Xác định paging number của trang, tính toán index chap
        /// </summary>
        public string PagingNumberRegex { get; set; }

        /// <summary>
        /// Số lượng post trên 1 paging category, example: 20
        /// </summary>
        public int AmountPostInCategory { get; set; } = 20;
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
