using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lsf.Models;

namespace Lsf.Client
{
    
    
    public class LsfClient
    {
        private static readonly CookieContainer CookieContainer = new CookieContainer();
        
        private static readonly HttpClient HttpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = CookieContainer,
            UseCookies = true,
        })
        {
            BaseAddress = new Uri("https://lsf.ovgu.de/")
        };

        public LsfClient(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        protected virtual bool RequiresAuthentication => false;

        protected string BaseUrl { get; }

        public async Task SetSemester(int year, SemesterType semesterType)
        {
            var part = semesterType == SemesterType.Summer ? 1 : 2;
            var url = Url(
                $"state=user&type=0&k_semester.semid={year}{part}&idcol=k_semester.semid&idval={year}{part}&purge=n&getglobal=semester");

            await GetAsync(url);
        }

        public async Task ClearSchedule()
        {
            await ReplaceSchedule();

        }

        public async Task ReplaceSchedule(params IWebScheduleComponent[] components)
        {
            var dynamicContent = components.Select(component =>
                new KeyValuePair<string, string>($"add.{component.EventId}", component.ScheduleId ?? "")).ToArray();
            
            var result = await PostAsyncHtml(Url("state=wplan&search=ver&act=add"),
                new FormUrlEncodedContent(dynamicContent));

            var submit = result.DocumentNode.QuerySelectorAll("input")
                .SingleOrDefault(node => node.Attributes["name"]?.Value == "PlanSpeichern");
            var form = FindParent(submit, node => node.Name == "form");
            var url = form?.Attributes["action"]?.Value;

            var staticContent = new Dictionary<string, string>
                {{"par", "old"}, {"from", "out"}, {"PlanSpeichern", "Plan Speichern"}};

            var content = staticContent.Concat(dynamicContent);

            await PostAsync(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(content));

            //par=old&from=out&PlanSpeichern=Plan+speichern&add.145581=3&add.144876=&add.144876=
        }

        private string Url(string queryParams)
        {
            return $"{BaseUrl}/qislsf/rds?{queryParams}";
        }


        public Task<HtmlDocument> GetMainPage()
        {
            return GetHtmlAsync(Url("state=user&type=0"));
        }

        public async Task<bool> Authenticate(string userName, string password)
        {
            var loginPage = await GetMainPage();
            var form = loginPage.DocumentNode.QuerySelectorAll("form").Single(node => node.Attributes["name"]?.Value == "loginform");

            var url = form?.Attributes["action"]?.Value;

            if (url is null)
            {
                return false;
            }

            var content = await PostAsyncHtml(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"asdf", userName}, {"fdsa", password}, {"submit", "Anmelden"}
            }));

            return content.DocumentNode.QuerySelectorAll("a")
                       .SingleOrDefault(node => node.Attributes["href"]?.Value.Contains("auth.logout") == true) != null;
        }

        public static async Task<string> PostAsync(string url, HttpContent content)
        {
            var result = await HttpClient.PostAsync(url, content);
            
            return await result.Content.ReadAsStringAsync();
        }

        private static async Task<HtmlDocument> PostAsyncHtml(string url, HttpContent content)
        {
            var response = await PostAsync(url, content);
            
            var document = new HtmlDocument();

            document.LoadHtml(response);

            return document;
        }

        private static HtmlNode FindParent(HtmlNode node, Predicate<HtmlNode> predicate)
        {
            while (node != null && !predicate(node))
            {
                node = node.ParentNode;
            }

            return node;
        }

        private static async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            var content = await GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            return doc;
        }

        private static async Task<string> GetAsync(string url)
        {
            var response = await HttpClient.GetAsync(url);

            return await response.Content.ReadAsStringAsync();
        }
    }
}