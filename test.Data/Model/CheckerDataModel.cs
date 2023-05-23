using DotnetCrawler.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Data.Model {
    public class CheckerDataModel {
        public Duplicate ChapDuplicate { get; set; }
        public Duplicate PostDuplicate { get; set; }
        public Duplicate CategoryDuplicate { get; set; }
    }

    public class Duplicate {
        public List<DuplicateRecord> DuplicateRecords { get; set; }
        public int Count { get; set; }
    }


    public class DuplicateRecord {
        public string Slug { get; set; }
        public int Count { get; set; }
    }
}
