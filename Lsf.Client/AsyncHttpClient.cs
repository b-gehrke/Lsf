using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Lsf.Client
{
    public class AsyncHttpClient : HttpClient
    {
        public AsyncHttpClient(string baseAddress) : base(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = CookieContainer,
            UseCookies = true,
        })
        {
            BaseAddress = new Uri(baseAddress);
        }

        protected static readonly CookieContainer CookieContainer = new CookieContainer();

        public async Task<string> PostStringAsync(string url, HttpContent content)
        {
            var result = await PostAsync(url, content);
            
            return await result.Content.ReadAsStringAsync();
        }

        public async Task<HtmlDocument> PostHtmlAsync(string url, HttpContent content)
        {
            var response = await PostStringAsync(url, content);
            
            var document = new HtmlDocument();

            document.LoadHtml(response);

            return document;
        }

        public Task<HtmlDocument> GetHtmlAsync(string url)
        {
            return GetHtmlAsync(url, CancellationToken.None);
        }


        public async Task<HtmlDocument> GetHtmlAsync(string url, CancellationToken token)
        {
            var response = await GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            return doc;
        }
    }
}