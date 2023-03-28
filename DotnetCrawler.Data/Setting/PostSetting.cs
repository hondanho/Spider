using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DotnetCrawler.Data.Setting {

    public class PostSetting : BaseSetting {
        /// <summary>
        /// string xpath title
        /// </summary>
        public string Titlte { get; set; }

        /// <summary>
        /// string xpth description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// string xpath slug
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// string xpath ảnh đại diện
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// bài viết con
        /// </summary>
        public bool IsHasChapter { get; set; }

        /// <summary>
        /// repace text: domain,.. -> domain của mình
        /// </summary>
        public Dictionary<string, string> StringReplace { get; set; }

        /// <summary>
        /// key,xpath ví dụ: tac-gia,
        /// </summary>
        public Dictionary<string, string> Taxonomies { get; set; } // như tac-gia, category // xpath string
        public Dictionary<string, string> Metadata { get; set; } // như tw_status // xpath string
        public List<string> RemoveElement { get; set; } // remove element như script, link, iframe, video

        public string LinkChapSelector { get; set; } // xpath string
        public string PagingSelector { get; set; } // xpath string

    }
}
