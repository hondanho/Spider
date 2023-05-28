namespace DotnetCrawler.Data.Models
{
    public interface IMongoDbSettings
    {
        string ConnectionString { get; set; }
    }

    public class MongoDbSettings : IMongoDbSettings
    {
        public string ConnectionString { get; set; }
    }
}
