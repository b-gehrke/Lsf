using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lsf.Client;
using Lsf.Models;
using Lsf.Parser;

namespace Lsf.Schedule
{
    public class LsfScheduleClient
    {
        private readonly LsfHttpClient _httpClient;
        private readonly EventParser _parser;

        public LsfScheduleClient(LsfHttpClient httpClient)
            : this(httpClient, new EventParser())
        {
            
        }
        
        public LsfScheduleClient(LsfHttpClient httpClient, EventParser parser)
        {
            _httpClient = httpClient;
            _parser = parser;
        }

        public async Task ClearSchedule()
        {
            await ReplaceSchedule();
        }

        public async Task ReplaceSchedule(params IWebScheduleComponent[] components)
        {
            var dynamicContent = components.Select(component =>
                new KeyValuePair<string, string>($"add.{component.EventId}", component.ScheduleId ?? "")).ToArray();

            var result = await _httpClient.PostHtmlAsync(_httpClient.Url("state=wplan&search=ver&act=add"),
                new FormUrlEncodedContent(dynamicContent));

            var submit = result.DocumentNode.QuerySelectorAll("input")
                .SingleOrDefault(node => node.Attributes["name"]?.Value == "PlanSpeichern");
            var form = FindParent(submit, node => node.Name == "form");
            var url = form?.Attributes["action"]?.Value;

            var staticContent = new Dictionary<string, string>
                {{"par", "old"}, {"from", "out"}, {"PlanSpeichern", "Plan Speichern"}};

            var content = staticContent.Concat(dynamicContent);

            await _httpClient.PostStringAsync(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(content));

            //par=old&from=out&PlanSpeichern=Plan+speichern&add.145581=3&add.144876=&add.144876=
        }

        public async Task<IEnumerable<Event>> Search(string name, Semester semester)
        {
            var urlPart =
                $"state=wsearchv&search=1&subdir=veranstaltung&veranstaltung.semester={semester}&veranstaltung.dtxt={name.Replace(' ', '+')}&P_start=0&P_anzahl=100&P.sort=veranstaltung.dtxt&_form=display";

            var content = await _httpClient.GetHtmlAsync(_httpClient.Url(urlPart));

            return null;
        }

        public async Task<Event> GetEvent(string eventId)
        {
            var urlPart = $"state=verpublish&status=init&vmfile=no&publishid={eventId}&moduleCall=webInfo&publishConfFile=webInfo&publishSubDir=veranstaltung";
            var document = await _httpClient.GetHtmlAsync(_httpClient.Url(urlPart));

            var links = _parser.GetIcalLinksFromDocument(document);

            var icals = await Task.WhenAll(links.Select(_httpClient.GetStringAsync));

            var calendars = icals.Select(source => _parser.ParseICalendar(source));

            return _parser.Parse(eventId, calendars);
        }


        private static HtmlNode FindParent(HtmlNode node, Predicate<HtmlNode> predicate)
        {
            while (node != null && !predicate(node))
            {
                node = node.ParentNode;
            }

            return node;
        }
    }
}