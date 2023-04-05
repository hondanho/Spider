using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DotnetCrawler.Data.Models
{
    public class Document : IDocument
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface IDocument
    {
        string Id { get; set; }

        DateTime CreatedAt { get; set; }
    }
}
