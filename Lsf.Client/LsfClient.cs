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
    public class LsfClient : AsyncHttpClient
    {
        public LsfClient(string baseUrl) : base(baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public bool IsAuthenticated { get; private set; }

        public string BaseUrl { get; }
        
        public Uri BaseUri => new Uri(BaseUrl);
        
        public string Url(string queryParams)
        {
            return $"{BaseUrl}/qislsf/rds?{queryParams}";
        }

        public Task<HtmlDocument> GetMainPage()
        {
            return GetHtmlAsync(GetMainPageUrl());
        }

        private string GetMainPageUrl()
        {
            return Url("state=user&type=0");
        }

        public async Task<bool> Authenticate(string jSessionId)
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

        public string CookieDomain => "." + BaseUrl.Split("/")[2];

        public async Task<bool> Authenticate(string userName, string password)
        {
            if (IsAuthenticated)
            {
                return true;
            }

            var loginPage = await GetMainPage();
            var form = loginPage.DocumentNode.QuerySelectorAll("form")
                .Single(node => node.Attributes["name"]?.Value == "loginform");

            var url = form?.Attributes["action"]?.Value;

            if (url is null)
            {
                return false;
            }

            var content = await PostHtmlAsync(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"asdf", userName}, {"fdsa", password}, {"submit", "Anmelden"}
                }));

            var result = VerifyAuthentication(content);

            return result;
        }

        private async Task<bool> VerifyAuthentication()
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
    }
}