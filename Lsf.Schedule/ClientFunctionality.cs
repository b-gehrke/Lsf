using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lsf.Client;
using Lsf.Models;

namespace Lsf.Schedule
{
    public static class ClientFunctionality
    {
        public static async Task ClearSchedule(this LsfClient client)
        {
            await client.ReplaceSchedule();
        }

        public static async Task ReplaceSchedule(this LsfClient client, params IWebScheduleComponent[] components)
        {
            var dynamicContent = components.Select(component =>
                new KeyValuePair<string, string>($"add.{component.EventId}", component.ScheduleId ?? "")).ToArray();

            var result = await client.PostHtmlAsync(client.Url("state=wplan&search=ver&act=add"),
                new FormUrlEncodedContent(dynamicContent));

            var submit = result.DocumentNode.QuerySelectorAll("input")
                .SingleOrDefault(node => node.Attributes["name"]?.Value == "PlanSpeichern");
            var form = FindParent(submit, node => node.Name == "form");
            var url = form?.Attributes["action"]?.Value;

            var staticContent = new Dictionary<string, string>
                {{"par", "old"}, {"from", "out"}, {"PlanSpeichern", "Plan Speichern"}};

            var content = staticContent.Concat(dynamicContent);

            await client.PostStringAsync(
                HtmlEntity.DeEntitize(url), new FormUrlEncodedContent(content));

            //par=old&from=out&PlanSpeichern=Plan+speichern&add.145581=3&add.144876=&add.144876=
        }

        public static async Task<IEnumerable<Event>> Search(string name, Semester semester)
        {
            var urlPart =
                $"state=wsearchv&search=1&subdir=veranstaltung&veranstaltung.semester={semester}&veranstaltung.dtxt={name.Replace(' ', '+')}&P_start=0&P_anzahl=100&P.sort=veranstaltung.dtxt&_form=display";


            return null;
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