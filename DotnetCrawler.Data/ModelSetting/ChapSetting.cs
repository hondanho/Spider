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

        /// <summary>
        /// remove element như script, link, iframe, video trên Contentm slug, title
        /// </summary>
        public List<string> RemoveElement { get; set; } // remove element như script, link, iframe, video

        /// <summary>
        ///  remove element by css selector
        /// </summary>
        public List<string> RemoveElementCssSelector { get; set; }

        public string PagingSelector { get; set; }
    }
}
