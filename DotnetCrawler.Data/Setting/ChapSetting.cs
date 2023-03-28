using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DotnetCrawler.Data.Setting {

    public class ChapSetting : BaseSetting {
        [Required]
        public string Titlte { get; set; }
        [Required]
        public string Content { get; set; }
        public string Slug { get; set; }
        public List<string> RemoveElement { get; set; } // remove element như script, link, iframe, video

        public string PagingSelector { get; set; }
    }
}
