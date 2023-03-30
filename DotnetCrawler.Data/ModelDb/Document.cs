using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.InteropServices;

namespace DotnetCrawler.Data.Models
{
    public class Document : IDocument
    {
        public ObjectId Id { get; set; }
        public string IdString
        {
            get
            {
                return Id.ToString() ?? string.Empty;
            }
        }
        public DateTime CreatedAt => Id.CreationTime;
    }

    public interface IDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        ObjectId Id { get; set; }

        DateTime CreatedAt { get; }
    }
}
