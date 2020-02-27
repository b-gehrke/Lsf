using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Lsf.Client
{
    public abstract class LsfHttpClient : AsyncHttpClient
    {
        public LsfHttpClient(string baseAddress) : base(baseAddress)
        {
        }

        public abstract bool IsAuthenticated { get; protected set; }
        public abstract string BaseUrl { get; }
        public abstract Uri BaseUri { get; }
        public abstract string Url(string queryParams);
        public abstract Task<HtmlDocument> GetMainPage();
        public abstract Task<bool> Authenticate(string jSessionId);
        public abstract Task<bool> Authenticate(string userName, string password);
    }

    public class LsfHttpClientImpl : LsfHttpClient
    {
        public LsfHttpClientImpl(string baseUrl) : base(baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public override bool IsAuthenticated { get; protected set; }

        public override string BaseUrl { get; }

        public override Uri BaseUri => new Uri(BaseUrl);

        public override string Url(string queryParams)
        {
            return $"{BaseUrl}/qislsf/rds?{queryParams}";
        }

        public override Task<HtmlDocument> GetMainPage()
        {
            return GetHtmlAsync(GetMainPageUrl());
        }

        private string GetMainPageUrl()
        {
            return Url("state=user&type=0&topitem=&breadCrumbSource=portal&topitem=functions");
        }

        public override async Task<bool> Authenticate(string jSessionId)
        {
            var cookies = CookieContainer.GetCookies(BaseUri);
            var jSessionIdCookie = cookies["JSESSIONID"] ?? new Cookie("JSESSIONID", jSessionId)
            {
                Path = "/qislsf", Secure = true, HttpOnly = true
            };

            jSessionIdCookie.Value = jSessionId;

            CookieContainer.Add(new Uri(GetMainPageUrl()), jSessionIdCookie);

            return await VerifyAuthentication();
        }

        public override async Task<bool> Authenticate(string userName, string password)
        {
            if (await VerifyAuthentication()) return true;

            var loginPage = await GetMainPage();
            var form = loginPage.DocumentNode.QuerySelectorAll("form")
                .Single(node => node.Attributes["name"]?.Value == "loginform");

            var url = form?.Attributes["action"]?.Value;

            if (url is null) return false;

            var content = await PostHtmlAsync(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"asdf", userName}, {"fdsa", password}, {"submit", "Anmelden"}
                }));

            var result = VerifyAuthentication(content);

            return result;
        }

        public async Task<bool> VerifyAuthentication()
        {
            var content = await GetMainPage();

            return VerifyAuthentication(content);
        }

        private bool VerifyAuthentication(HtmlDocument content)
        {
            var result = content.DocumentNode.QuerySelectorAll("a")
                             .SingleOrDefault(node => node.Attributes["href"]?.Value.Contains("auth.logout") == true) !=
                         null;

            IsAuthenticated = result;
            return result;
        }

        public string GetLoginCookie()
        {
            return CookieContainer.GetCookies(new Uri(GetMainPageUrl())).FirstOrDefault()?.Value;
        }
    }
}