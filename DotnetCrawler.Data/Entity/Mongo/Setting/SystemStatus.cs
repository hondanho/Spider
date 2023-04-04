
namespace DotnetCrawler.Data.Setting {

    public class SystemStatus
    {
        public string JobId { get; set; }
        public StatusCrawler Status { get; set; }
    }

    public enum StatusCrawler
    {
        DEFAULT = 1,
        CRAWLER_DOING = 2,
        DATHUTHAP = 3,
        RECRAWLER_BIG_DOING = 4,
        RECRAWLER_SMALL_DOING = 5
    }
}
