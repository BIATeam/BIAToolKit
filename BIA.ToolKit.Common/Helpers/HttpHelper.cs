namespace BIA.ToolKit.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public static class HttpHelper
    {
        private static HttpClient httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    httpClient = new HttpClient(
                        new HttpClientHandler
                        {
                            UseProxy = true,
                            DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                        },
                        disposeHandler: true);

                    string appName = BiaToolkitVersion.ApplicationName;
                    string appVersion = BiaToolkitVersion.ApplicationVersion;
                    string framework = RuntimeInformation.FrameworkDescription.Trim();
                    string os = RuntimeInformation.OSDescription.Trim();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", $"{appName}/{appVersion} ({os}; {framework})");
                }

                return httpClient;
            }
        }
    }
}
