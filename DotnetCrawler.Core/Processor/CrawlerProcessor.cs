using DotnetCrawler.Base.Extension;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Processor
{
    public class CrawlerProcessor : ICrawlerProcessor
    {
        private readonly SiteConfigDb _request;

        public CrawlerProcessor(SiteConfigDb request) {
            _request = request;
        }

        public async Task<PostDb> PostProcess(string categoryId, string url, HtmlDocument document)
        {
            // remove element by css selector
            if(_request.PostSetting.RemoveElementCssSelector != null && _request.PostSetting.RemoveElementCssSelector.Count > 0) {
                foreach(string cssSelector in _request.PostSetting.RemoveElementCssSelector) {
                    var nodesToRemove = document.DocumentNode.QuerySelectorAll(cssSelector);
                    if(nodesToRemove != null) {
                        foreach(HtmlNode node in nodesToRemove) {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            // remove node element
            if(_request.PostSetting.RemoveNodeElement != null && _request.PostSetting.RemoveNodeElement.Count > 0) {
                foreach(string nodeStr in _request.PostSetting.RemoveNodeElement) {
                    HtmlNodeCollection nodesToRemove = document.DocumentNode.SelectNodes($"//{nodeStr}");
                    if(nodesToRemove != null) {
                        foreach(HtmlNode node in nodesToRemove) {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

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
            if (!Helper.IsValidURL(avatar)) avatar = _request.BasicSetting.Domain + avatar;
            var entity = new PostDb()
            {
                CategoryId = categoryId,
                Titlte = entityNode.QuerySelector(_request.PostSetting.Titlte)?.InnerText,
                Slug = (new Uri(url)).AbsolutePath,
                Description = entityNode.QuerySelector(_request.PostSetting.Description)?.InnerText,
                Avatar = avatar,
                Taxonomies = taxonomies,
                Metadata = metadata
            };

            return entity;
        }

        public async Task<ChapDb> ChapProcess(string postId, string url, HtmlDocument document)
        {
            // remove element by css selector
            if(_request.PostSetting.RemoveElementCssSelector != null && _request.PostSetting.RemoveElementCssSelector.Count > 0) {
                foreach(string cssSelector in _request.PostSetting.RemoveElementCssSelector) {
                    var nodesToRemove = document.DocumentNode.QuerySelectorAll(cssSelector);
                    if(nodesToRemove != null) {
                        foreach(HtmlNode node in nodesToRemove) {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            // replace element
            if(_request.ChapSetting.RemoveElement != null && _request.ChapSetting.RemoveElement.Count > 0) {
                foreach(string nodeStr in _request.ChapSetting.RemoveElement) {
                    HtmlNodeCollection nodesToRemove = document.DocumentNode.SelectNodes($"//{nodeStr}");
                    if(nodesToRemove != null) {
                        foreach(HtmlNode node in nodesToRemove) {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            var entityNode = document.DocumentNode;
            var entity = new ChapDb()
            {
                PostId = postId,
                Titlte = entityNode.QuerySelector(_request.ChapSetting.Titlte)?.InnerText,
                Content = entityNode.QuerySelector(_request.ChapSetting.Content)?.InnerText,
                Slug = (new Uri(url)).AbsolutePath
            };

            return entity;
        }
    }
}
