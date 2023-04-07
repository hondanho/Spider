using DotnetCrawler.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Data.Model {
    public class CountSiteInfo {
        public long Category { get; set; }
        public long Post { get; set; }
        public long Chap { get; set; }
    }

    public class CategoryInfo {
        public CategoryDb CategoryDb { get; set; }
        public List<PostInfo> PostInfos { get; set; }
        public int PostCount { get; set; }
    }

    public class PostInfo {
        public PostDb PostDb { get; set; }
        public int ChapCount { get; set; }
        public List<ChapDb> ChapInfos { get; set; }
    }
}
