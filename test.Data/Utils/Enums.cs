using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DotnetCrawler.Data.Utils {
    public enum DatePreset {
        [Description("Today")]
        Today = 0,

        [Description("Yesterday")]
        Yesterday = 1,

        [Description("Last3Day")]
        Last3Day = 3,

        [Description("Last7Day")]
        Last7Day =7,

        [Description("Last30Day")]
        Last30Day = 30
    }
}
