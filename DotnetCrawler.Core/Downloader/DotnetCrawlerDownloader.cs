using DotnetCrawler.Base.Extension;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DotnetCrawler.Downloader
{
    public class DotnetCrawlerDownloader
    {

        public static async Task<HtmlDocument> Download(string crawlUrl, string DownloadPath, List<string> Proxys, string userAgent, DotnetCrawlerDownloaderType DownloderType)
        {
            // if exist dont download again
            var parts = crawlUrl.Split('/');
            parts = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var htmlpage = string.Empty;
            if (parts.Length > 0)
            {
                htmlpage = parts[parts.Length - 1];
            }

            if (!htmlpage.Contains(".html"))
            {
                htmlpage = htmlpage + ".html";
            }
            htmlpage = htmlpage.Replace("=", "").Replace("?", "");

            var localFilePath = $"{DownloadPath}{htmlpage}";

            var existing = GetExistingFile(localFilePath);
            if (existing != null)
                return existing;

            return await DownloadInternal(crawlUrl, Proxys, userAgent, localFilePath, DownloderType);
        }

        private static async Task<HtmlDocument> DownloadInternal(string crawlUrl, List<string> Proxys, string userAgent, string localFilePath, DotnetCrawlerDownloaderType DownloderType)
        {
            var webProxy = Helper.GetRandomProxyLiveOrDefault(Proxys);

            switch (DownloderType)
            {
                case DotnetCrawlerDownloaderType.FromFile:
                    using (WebClient client = new WebClient())
                    {
                        client.Proxy = webProxy;
                        await client.DownloadFileTaskAsync(crawlUrl, localFilePath);
                    }
                    return GetExistingFile(localFilePath);

                case DotnetCrawlerDownloaderType.FromMemory:
                    var htmlDocument = new HtmlDocument();
                    using (WebClient client = new WebClient())
                    {
                        //client.Headers.Add(userAgent);
                        client.Proxy = webProxy;
                        string htmlCode = await client.DownloadStringTaskAsync(crawlUrl);
                        htmlDocument.LoadHtml(htmlCode);
                    }
                    return htmlDocument;

                case DotnetCrawlerDownloaderType.FromWeb:
                    HtmlWeb web = new HtmlWeb();
                    return await web.LoadFromWebAsync(crawlUrl);
            }

            throw new InvalidOperationException("Can not load html file from given source.");
        }

        private static HtmlDocument GetExistingFile(string fullPath)
        {
            try
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(fullPath);
                return htmlDocument;
            }
            catch (Exception exception)
            {
            }
            return null;
        }

    }
}
