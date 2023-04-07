
using System;

namespace DotnetCrawler.Data.Models
{
    public class Document : IDocument
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        //public bool IsSynced { get; set; } = false;
        //public int IdPostWpSynced { get; set; }
    }

    public interface IDocument
    {
        string Id { get; set; }
        DateTime CreatedAt { get; set; }
        //bool IsSynced { get; set; }
        //public int IdPostWpSynced { get; set; }
    }
}
