using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DotnetCrawler.Data.Setting {

    public class CategorySetting : BaseSetting {
        public string Domain { get; set; }
        public long TimeOut { get; set; }
        public string Titlte { get; set; }
        public string Url { get; set; }
        public string Slug { get; set; }

        public string LinkPostSelector { get; set; }
        public string PagingSelector { get; set; }
    }
}
