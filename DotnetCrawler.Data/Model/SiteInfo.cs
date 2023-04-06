using DotnetCrawler.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Data.Model {
    public class CategoryInfo {
        public CategoryDb CategoryDb { get; set; }
        public List<PostInfo> PostInfos { get; set; }
        public int PostCount { get; set; }
    }

    public class PostInfo {
        public PostDb PostDb { get; set; }
        public int ChapCount { get; set; }
    }
}
