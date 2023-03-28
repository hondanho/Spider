using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DotnetCrawler.Data.Models.Novel {

    public class CategoryDb : BaseEntity {
        public string Domain { get; set; }
        public string Titlte { get; set; }
        public string Url { get; set; }
        public string Slug { get; set; }
    }
}
