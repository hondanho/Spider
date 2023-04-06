using DotnetCrawler.Base.Extension;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Downloader
{
    public class DotnetCrawlerPageLinkReader
    {

        public static async Task<IEnumerable<string>> GetLinks(HtmlDocument document, string cssSelectorLink, string domain, int level = 0)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));

           var rootUrls = document.DocumentNode.QuerySelectorAll(cssSelectorLink)
                                .Select(a =>
                                {
                                    string href = a.GetAttributeValue("href", null);
                                    if (!Helper.IsValidURL(href))
                                    {
                                        return domain + href;
                                    }

                                    return href;
                                })
                                .Where(u => !string.IsNullOrEmpty(u))
                                .Distinct().ToList();

            if (level == 0)
                return rootUrls;

            var links = await GetAllPagesLinks(rootUrls, cssSelectorLink, domain);

            --level;
            var tasks = await Task.WhenAll(links.Select(link => GetLinks(link, cssSelectorLink, domain, level)));
            return tasks.SelectMany(l => l);
        }


        public static async Task<IEnumerable<string>> GetLinks(string url, string cssSelectorLink, string domain, int level = 0)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));

            var rootUrls = await GetPageLinks(url, cssSelectorLink, domain, false);

            if (level == 0)
                return rootUrls;

            var links = await GetAllPagesLinks(rootUrls, cssSelectorLink, domain);

            --level;
            var tasks = await Task.WhenAll(links.Select(link => GetLinks(link, cssSelectorLink, domain, level)));
            return tasks.SelectMany(l => l);
        }

        private static async Task<IEnumerable<string>> GetPageLinks(string url, string cssSelectorLink, string domain, bool needMatch = true) {
            try {
                HtmlWeb web = new HtmlWeb();
                var htmlDocument = await web.LoadFromWebAsync(url);

                var linkList = htmlDocument.DocumentNode.QuerySelectorAll(cssSelectorLink)
                                .Select(a => {
                                    string href = a.GetAttributeValue("href", null);
                                    if(!Helper.IsValidURL(href)) {
                                        return domain + href;
                                    }

                                    return href;
                                })
                                .Where(u => !string.IsNullOrEmpty(u))
                                .Distinct().ToList();
                return linkList;
            } catch(Exception exception) {
                return Enumerable.Empty<string>();
            }
        }

        public static async Task<IEnumerable<LinkModel>> GetPageLinkModel(HtmlDocument document, string cssSelectorLink, string domain) {
            try
            {
                var linkList = new List<LinkModel>();

                var htmlNodes = document.DocumentNode.QuerySelectorAll(cssSelectorLink);
                foreach(var node in htmlNodes) {
                    string href = node.GetAttributeValue("href", null);
                    if(!Helper.IsValidURL(href)) {
                        href = domain + href;
                    }

                    var slug = (new Uri(href)).AbsolutePath;
                    if (!string.IsNullOrEmpty(node.InnerText) && !string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(slug)) {
                        linkList.Add(new LinkModel() {
                            Titlte = node.InnerText,
                            Url = href,
                            Slug = slug
                        });
                    }
                }

                return linkList.Distinct();
            }
            catch (Exception exception)
            {
                return Enumerable.Empty<LinkModel>();
            }
        }

        private static async Task<IEnumerable<string>> GetAllPagesLinks(IEnumerable<string> rootUrls, string cssSelectorLink, string domain)
        {
            var result = await Task.WhenAll(rootUrls.Select(url => GetPageLinks(url, cssSelectorLink, domain)));

            return result.SelectMany(x => x).Distinct();
        }
    }
}
