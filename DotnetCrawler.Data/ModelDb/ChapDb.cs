﻿using DotnetCrawler.Data.Attributes;

namespace DotnetCrawler.Data.Models
{

    [BsonCollection("chap")]
    public class ChapDb : Document
    {
        public string Titlte { get; set; }
        public string Content { get; set; }
        public string Slug { get; set; }
    }
}
