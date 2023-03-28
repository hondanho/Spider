using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DotnetCrawler.Data.Models.Novel {

    public class PostDb : BaseEntity {
        public string Titlte { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public string Avatar { get; set; }
        public string Taxonomies { get; set; } // json data
        public string Metadata { get; set; } // json data
    }
}
