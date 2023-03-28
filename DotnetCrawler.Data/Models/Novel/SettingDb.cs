using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DotnetCrawler.Data.Models.Novel {

    public class SettingDb : BaseEntity {
       
        public bool CheckDuplicateUrlPost { get; set; }
        public bool CheckDuplicateTitlePost { get; set; }
        public bool CheckDuplicateUrlChapter { get; set; }
        public bool CheckDuplicateTitleChapter { get; set; }
        public bool IsThuThap { get;set; }
    }
}
