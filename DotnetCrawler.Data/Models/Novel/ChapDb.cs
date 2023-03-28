using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DotnetCrawler.Data.Models.Novel {

    public class ChapDb : BaseEntity {
        public string Titlte { get; set; }
        public string Content { get; set; }
        public string Slug { get; set; }
    }
}
