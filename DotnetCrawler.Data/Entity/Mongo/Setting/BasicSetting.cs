
using System.Collections.Generic;

namespace DotnetCrawler.Data.Setting {

    public class BasicSetting {
        public string Name { get; set; }
        public bool CheckDuplicateSlugPost { get; set; }
        public bool CheckDuplicateTitlePost { get; set; }
        public bool CheckDuplicateSlugChap { get; set; }
        public bool CheckDuplicateTitleChap { get; set; }
        public bool IsThuThap { get; set; }
        public bool IsThuThapLai { get; set; }
        public string Document { get; set; }
        public string Domain { get; set; }
        public List<string> Proxys { get; set; }
        public string WordpressUriApi { get; set; }
        public string WordpressUserName { get; set; }
        public string WordpressPassword { get; set; }
    }
}
