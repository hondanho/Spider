using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace DotnetCrawler.Core.Extension {
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
    }
}
