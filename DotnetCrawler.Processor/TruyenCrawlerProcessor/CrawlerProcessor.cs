using DotnetCrawler.Data.Models.Novel;
using DotnetCrawler.Request;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotnetCrawler.Processor
{
    public class CrawlerProcessor : ICrawlerProcessor
    {
        private readonly IDotnetCrawlerRequest _request;

        public CrawlerProcessor(IDotnetCrawlerRequest request) {
            _request = request;
        }

        public async Task<PostDb> PostProcess(HtmlDocument document)
        {
            var entityNode = document.DocumentNode;

            // get metadata
            var metadata = string.Empty;
            if (_request.PostSetting.Metadata != null && _request.PostSetting.Metadata.Count > 0)
            {
                var dataDict = _request.PostSetting.Metadata.Select(kvp => new KeyValuePair<string, string>(kvp.Key, entityNode.QuerySelector(kvp.Value)?.InnerText));
                if (dataDict.Any())
                {
                    metadata = JsonConvert.SerializeObject(dataDict);
                }
            }

            var taxonomies = string.Empty;
            if (_request.PostSetting.Taxonomies != null && _request.PostSetting.Taxonomies.Count > 0)
            {
                var dataDict = _request.PostSetting.Taxonomies.Select(kvp => new KeyValuePair<string, string>(kvp.Key, entityNode.QuerySelector(kvp.Value)?.InnerText));
                if (dataDict.Any())
                {
                    taxonomies = JsonConvert.SerializeObject(dataDict);
                }
            }

            var avatar = entityNode.QuerySelector(_request.PostSetting.Avatar)?.GetAttributeValue("src", null);
            if (!IsValidURL(avatar)) avatar = _request.CategorySetting.Domain + avatar;

            var entity = new PostDb()
            {
                Titlte = entityNode.QuerySelector(_request.PostSetting.Titlte)?.InnerText,
                Description = entityNode.QuerySelector(_request.PostSetting.Description)?.InnerText,
                Avatar = avatar,
                Taxonomies = taxonomies,
                Metadata = metadata
            };

            return entity;
        }

        public async Task<ChapDb> ChapProcess(HtmlDocument document)
        {
            var entityNode = document.DocumentNode;

            var entity = new ChapDb()
            {
                Titlte = entityNode.QuerySelector(_request.ChapSetting.Titlte)?.InnerText,
                Content = entityNode.QuerySelector(_request.ChapSetting.Content)?.InnerText,
                Slug = entityNode.QuerySelector(_request.ChapSetting.Slug)?.InnerText
            };

            return entity;
        }

        private bool IsValidURL(string URL)
        {
            string Pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return Rgx.IsMatch(URL);
        }
    }
}
