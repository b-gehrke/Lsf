using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Lsf.Client.Parser
{
    public abstract class WebParser
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        protected WebParser(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        protected virtual bool RequiresAuthentication => false;

        protected string BaseUrl { get; }

        protected Task<HtmlDocument> GetHtmlAsync(string url)
        {
            var htmlWeb = new HtmlWeb();
            return htmlWeb.LoadFromWebAsync(url);
        }

        protected Task<string> GetAsync(string url)
        {
            return _httpClient.GetStringAsync(url);
        }
    }
}