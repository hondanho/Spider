using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotnetCrawler.Base.Extension {
    public  class Helper {

        public static T GetRandomElement<T>(List<T> values) {
           if (values != null && values.Count > 0) {
                Random rand = new Random();
                int index = rand.Next(values.Count);
                return values[index];
            }

            return default(T);
        }

        public static string GetRandomProxyLive(List<string> proxys) {
            var result = string.Empty;
            while (true && proxys != null && proxys.Count > 0) {
                var proxy = GetRandomElement<string>(proxys);
                if (!string.IsNullOrEmpty(proxy) && CheckStatusProxyLive(proxy)) {
                    result = proxy;
                    break;
                } else {
                    proxys.Remove(proxy);
                }
            }

            return result;
        }

        public static bool CheckStatusProxyLive(string proxyAddress) {
            try {
                WebRequest request = WebRequest.Create("http://www.google.com");
                request.Proxy = GetWebProxy(proxyAddress);
                WebResponse response = request.GetResponse();
                return true;
            } catch {
                return false;
            }
        }

        public static WebProxy GetRandomProxyLiveOrDefault(List<string> proxys) {
            var proxyAddress = GetRandomProxyLive(proxys);
            if(string.IsNullOrEmpty(proxyAddress))
                return new WebProxy();
            else {
                return GetWebProxy(proxyAddress);
            }
        }

        public static WebProxy GetWebProxy(string proxyAddress) {
            WebProxy proxyString;
            if(proxyAddress.Split(":").Length == 4) {
                var proxyIpPort = proxyAddress.Split(":");
                proxyString = new WebProxy(String.Format("{0}:{1}", proxyIpPort[0], proxyIpPort[1]), true);
                proxyString.Credentials = new NetworkCredential(proxyIpPort[2], proxyIpPort[3]);
            } else {
                proxyString = new WebProxy(proxyAddress, true);
            }

            return proxyString;
        }

        public static bool IsValidURL(string URL) {
            string Pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return Rgx.IsMatch(URL);
        }

        public static void DownloadImage(string imageUrl, string savePath, string fileName)
        {
            // Kiểm tra xem thư mục đã tồn tại chưa
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(imageUrl, Path.Combine(savePath, fileName));
                Console.WriteLine("Tải xuống thành công!");
            }
        }

        public static async Task<string> GetTextFromSelector(string cssSelector, string url)
        {
            var htmlDocument = new HtmlDocument();
            using (WebClient client = new WebClient())
            {
                string htmlCode = await client.DownloadStringTaskAsync(url);
                htmlDocument.LoadHtml(htmlCode);
                return htmlDocument.QuerySelector(cssSelector)?.InnerText;
            }
        }

        public static async Task<List<string>> GetTextsFromSelector(string cssSelector, string url)
        {
            var htmlDocument = new HtmlDocument();
            var result = new List<string>();

            using (WebClient client = new WebClient())
            {
                string htmlCode = await client.DownloadStringTaskAsync(url);
                htmlDocument.LoadHtml(htmlCode);
                var allnodes = htmlDocument.QuerySelectorAll(cssSelector);
                foreach (var node in allnodes)
                {
                    result.Add(node.InnerText);
                }
            }

            return result;
        }

        public static async Task<string> GetUrlImageFromSelector(string cssSelector, string url)
        {
            var htmlDocument = new HtmlDocument();
            using (WebClient client = new WebClient())
            {
                string htmlCode = await client.DownloadStringTaskAsync(url);
                htmlDocument.LoadHtml(htmlCode);
                return htmlDocument.QuerySelector(cssSelector)?.GetAttributeValue("src", null);
            }
        }
    }
}
