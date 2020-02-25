using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Lsf.Client.Parser
{
    public abstract class WebParser
    {
        private readonly LsfHttpClient _httpClient;

        protected WebParser(LsfHttpClient httpClient)
        {
            _httpClient = httpClient;
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