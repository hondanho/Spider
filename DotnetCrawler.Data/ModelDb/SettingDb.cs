using DotnetCrawler.Data.Attributes;

namespace DotnetCrawler.Data.Models
{

    [BsonCollection("SettingDb")]
    public class SettingDb : Document
    {

        public bool CheckDuplicateUrlPost { get; set; }
        public bool CheckDuplicateTitlePost { get; set; }
        public bool CheckDuplicateUrlChapter { get; set; }
        public bool CheckDuplicateTitleChapter { get; set; }
        public bool IsThuThap { get; set; }
    }
}
