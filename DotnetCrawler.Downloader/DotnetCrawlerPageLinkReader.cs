using DotnetCrawler.Request;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotnetCrawler.Downloader
{
    /// <summary>
    /// Get Urls
    // https://codereview.stackexchange.com/questions/139783/web-crawler-that-uses-task-parallel-library 
    /// </summary>
    public class DotnetCrawlerPageLinkReader
    {
        private readonly IDotnetCrawlerRequest _request;

        public DotnetCrawlerPageLinkReader(IDotnetCrawlerRequest request)
        {
            _request = request;
        }

        public async Task<IEnumerable<string>> GetLinks(string url, string cssSelectorLink, int level = 0)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));

            var rootUrls = await GetPageLinks(url, cssSelectorLink, false);

            if (level == 0)
                return rootUrls;

            var links = await GetAllPagesLinks(rootUrls, cssSelectorLink);

            --level;
            var tasks = await Task.WhenAll(links.Select(link => GetLinks(link, cssSelectorLink, level)));
            return tasks.SelectMany(l => l);
        }

        private async Task<IEnumerable<string>> GetPageLinks(string url, string cssSelectorLink, bool needMatch = true)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                var htmlDocument = await web.LoadFromWebAsync(url);
               
                var linkList = htmlDocument.DocumentNode.QuerySelectorAll(cssSelectorLink)
                                .Select(a => {
                                    string href = a.GetAttributeValue("href", null);
                                    if (!IsValidURL(href)) {
                                        return _request.CategorySetting.Domain + href;
                                    }

                                    return href;
                                })
                                .Where(u => !string.IsNullOrEmpty(u))
                                .Distinct().ToList();
                return linkList;
            }
            catch (Exception exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        private async Task<IEnumerable<string>> GetAllPagesLinks(IEnumerable<string> rootUrls, string cssSelectorLink)
        {
            var result = await Task.WhenAll(rootUrls.Select(url => GetPageLinks(url, cssSelectorLink)));

            return result.SelectMany(x => x).Distinct();
        }

        bool IsValidURL(string URL) {
            string Pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return Rgx.IsMatch(URL);
        }
    }
}
