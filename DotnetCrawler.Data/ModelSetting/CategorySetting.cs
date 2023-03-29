using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Data.Setting {

    public class CategorySetting : BaseSetting {
        [Required]
        public string Domain { get; set; }
        public long TimeOut { get; set; }
        [Required]
        public string Titlte { get; set; }
        [Required]
        public string Url { get; set; }
        public string Slug { get; set; }

        [Required]
        public string LinkPostSelector { get; set; }
        [Required]
        public string PagingSelector { get; set; }
    }
}
