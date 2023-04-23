using DotnetCrawler.Base.Extension;
using DotnetCrawler.Data.Constants;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL.Models;

namespace DotnetCrawler.Processor
{
    public class CrawlerProcessor
    {
        private const string PathDictSaveImage = @"D:/crawler";

        public static async Task<PostDb> PostProcess(SiteConfigDb _request, string categorySlug, string url, int index, HtmlDocument document, string documentName)
        {
            // remove element by css selector
            if (_request.PostSetting.RemoveElementCssSelector != null && _request.PostSetting.RemoveElementCssSelector.Count > 0)
            {
                foreach (string cssSelector in _request.PostSetting.RemoveElementCssSelector)
                {
                    var nodesToRemove = document.DocumentNode.QuerySelectorAll(cssSelector);
                    if (nodesToRemove != null)
                    {
                        foreach (HtmlNode node in nodesToRemove)
                        {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            // remove node element
            if (_request.PostSetting.RemoveNodeElement != null && _request.PostSetting.RemoveNodeElement.Any())
            {
                foreach (string nodeStr in _request.PostSetting.RemoveNodeElement)
                {
                    HtmlNodeCollection nodesToRemove = document.DocumentNode.SelectNodes($"//{nodeStr}");
                    if (nodesToRemove != null)
                    {
                        foreach (HtmlNode node in nodesToRemove)
                        {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            var entityNode = document.DocumentNode;
            var title = entityNode.QuerySelector(_request.PostSetting.Titlte)?.InnerText;
            var description = entityNode.QuerySelector(_request.PostSetting.Description)?.InnerText;
            var metadatas = new Dictionary<string, List<string>>();
            // get metadata
            if (_request.PostSetting.Metadatas != null && _request.PostSetting.Metadatas.Any())
            {
                foreach (var fieldPost in _request.PostSetting.Metadatas)
                {
                    var dataFieldPost = new List<string>();
                    foreach (var nodeMetadata in entityNode.QuerySelectorAll(fieldPost.Value))
                    {
                        if (fieldPost.RemoveElement != null && fieldPost.RemoveElement.Any())
                        {
                            foreach (string nodeStr in fieldPost.RemoveElement)
                            {
                                HtmlNodeCollection nodesToRemove = nodeMetadata.SelectNodes($"//{nodeStr}");
                                if (nodesToRemove != null)
                                {
                                    foreach (HtmlNode node in nodesToRemove)
                                    {
                                        node.Remove(); // Remove each selected node
                                    }
                                }
                            }
                        }

                        dataFieldPost.Add(nodeMetadata.InnerText);
                    }

                    metadatas.Add(fieldPost.Key, dataFieldPost);
                }
            }

            var slug =  (new Uri(url)).AbsolutePath;
            slug = Helper.CleanSlug(slug);
            var statusCurrent = metadatas.FirstOrDefault(metadata =>
                        metadata.Key == MetaFieldPost.Status &&
                        metadata.Value != null &&
                        metadata.Value.Any()
                    ).Value?.FirstOrDefault();

            var avatar = entityNode.QuerySelector(_request.PostSetting.Avatar)?.GetAttributeValue("src", null);
            if (!string.IsNullOrEmpty(avatar))
            {
                if (!Helper.IsValidURL(avatar)) avatar = _request.BasicSetting.Domain + avatar;
                Uri uriAvatar = new Uri(avatar);
                string filePath = uriAvatar.AbsolutePath;
                string fileName = Path.GetFileName(filePath);
                var pathSave = string.Format("{0}/{1}/{2}{3}", PathDictSaveImage, documentName, categorySlug, slug);
                
                if (!File.Exists(string.Format("{0}/{1}", pathSave, fileName)))
                {
                    Helper.DownloadImage(avatar, pathSave, fileName);
                }
                avatar = string.Format("{0}/{1}", pathSave, fileName);
            }
            var entity = new PostDb()
            {
                CategorySlug = categorySlug,
                Titlte = title,
                Slug = slug,
                Description = description,
                Status = Helper.ConvertStrToCapitalize(statusCurrent),
                Avatar = avatar,
                Metadatas = metadatas,
                Index = index
            };

            return entity;
        }

        public static async Task<ChapDb> ChapProcess(SiteConfigDb _request, string postSlug, string url, int index, HtmlDocument document)
        {
            // remove element by css selector
            if (_request.PostSetting.RemoveElementCssSelector != null && _request.PostSetting.RemoveElementCssSelector.Count > 0)
            {
                foreach (string cssSelector in _request.PostSetting.RemoveElementCssSelector)
                {
                    var nodesToRemove = document.DocumentNode.QuerySelectorAll(cssSelector);
                    if (nodesToRemove != null)
                    {
                        foreach (HtmlNode node in nodesToRemove)
                        {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            // replace element
            if (_request.ChapSetting.RemoveElement != null && _request.ChapSetting.RemoveElement.Count > 0)
            {
                foreach (string nodeStr in _request.ChapSetting.RemoveElement)
                {
                    HtmlNodeCollection nodesToRemove = document.DocumentNode.SelectNodes($"//{nodeStr}");
                    if (nodesToRemove != null)
                    {
                        foreach (HtmlNode node in nodesToRemove)
                        {
                            node.Remove(); // Remove each selected node
                        }
                    }
                }
            }

            var entityNode = document.DocumentNode;
            var slug = (new Uri(url)).AbsolutePath;
            slug = Helper.CleanSlug(slug);

            var entity = new ChapDb()
            {
                PostSlug = postSlug,
                Titlte = entityNode.QuerySelector(_request.ChapSetting.Titlte)?.InnerText,
                Content = entityNode.QuerySelector(_request.ChapSetting.Content)?.InnerText,
                Slug = slug,
                Index = index
            };

            return entity;
        }
    }
}
