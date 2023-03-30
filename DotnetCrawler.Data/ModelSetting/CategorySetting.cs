using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Data.Setting {

    public class CategorySetting : BaseSetting {
        /// <summary>
        /// Domain spy
        /// </summary>
        [Required]
        public string Domain { get; set; }
        public long TimeOut { get; set; }

        /// <summary>
        /// Set category name
        /// </summary>
        [Required]
        public string Titlte { get; set; }
        
        /// <summary>
        /// Url contains list post
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// Slug to db, if not exist then create it
        /// </summary>
        public string Slug { get; set; }

        [Required]
        public string LinkPostSelector { get; set; }
        [Required]
        public string PagingSelector { get; set; }
    }
}
