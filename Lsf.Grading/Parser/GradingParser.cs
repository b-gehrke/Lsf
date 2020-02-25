using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lsf.Client;
using Lsf.Client.Parser;
using Lsf.Grading.Models;
using Lsf.Models;
using Microsoft.VisualBasic;

namespace Lsf.Grading.Parser
{
    public class GradingParser
    {
        public LsfHttpClient HttpClient { get; }

        public GradingParser(LsfHttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<IEnumerable<Degree>> GetGradesForAllDegrees()
        {
            var mainPage = await HttpClient.GetMainPage();
            var link =
                HtmlEntity.DeEntitize(mainPage.QuerySelector("#makronavigation a[href*='studyPOSMenu']")?.Attributes["href"].Value);

            if (link == null)
            {
                throw new Exception("Couldn't find the link to 'Prüfungsverwaltung'!");
            }

            var content = await HttpClient.GetHtmlAsync(link);
            link = HtmlEntity.DeEntitize(content.QuerySelector("a[href*='notenspiegel']")?.Attributes["href"].Value);


            content = await HttpClient.GetHtmlAsync(link);

            var links = content.QuerySelectorAll(".treelist a[href*='list.vm']");
            var degreeParseTasks = links.Select(l =>
                ParseDegree(HtmlEntity.DeEntitize(l.Attributes["href"].Value), HtmlEntity.DeEntitize(l.InnerText)));

            var degrees = await Task.WhenAll(degreeParseTasks);

            return degrees;
        }

        private async Task<Degree> ParseDegree(string url, string name)
        {
            var content = await HttpClient.GetHtmlAsync(url);
            var degreeText = content
                .QuerySelectorAll(".content > form > table tr")
                .SingleOrDefault(x => x.ChildNodes.Count == 1)?.InnerText;

            var match = degreeText == null ? null : Regex.Match(degreeText,
                @"\[(?<DegreeId>\d+)\] (?<DegreeName>[\w\s]+): \[(?<MajorId>\d+)\] (?<MajorName>[\w\s]+)");

            var major = new Major
            {
                Id = match?.Groups["MajorId"]?.Value,
                Name = match?.Groups["MajorName"]?.Value,
            };

            var degree = new Degree
            {
                Id = match?.Groups["DegreeId"]?.Value,
                Name = match?.Groups["DegreeName"]?.Value,
                GradingMajors = new []{major}
            };

            var examRows = content.QuerySelectorAll(".content > form > table:nth-child(5) tr").Where(row =>
                row.Descendants("td").Count() == 9 &&
                row.Descendants("td").FirstOrDefault()?.HasClass("qis_konto") == false &&
                row.Descendants("td").FirstOrDefault()?.HasClass("qis_kontoOnTop") == false);

            var exams = examRows.Select(row => new ExamResult
            {
                Date = ParseColumnDateTime(row, 8),
                Grade = ParseColumnFloat(row, 3),
                Name = ParseColumnText(row, 1),
                Semester = Semester.Parse(ParseColumnText(row, 2)),
                Try = ParseColumnInt(row, 7),
                ExamNumber = ParseColumnText(row, 0),
                ExamState = ParseExamState(ParseColumnText(row, 4))
            });

            major.Gradings = exams.ToArray();

            return degree;
        }

        private static string ParseColumnText(HtmlNode row, int index)
        {
            return row.Descendants("td").ToArray()[index].InnerText.Trim();
        }

        private static DateTime ParseColumnDateTime(HtmlNode row, int index)
        {
            var text = ParseColumnText(row, index);

            return DateTime.TryParse(text, new CultureInfo("de-DE"), DateTimeStyles.None, out var result) ? result : DateTime.MinValue;
        }

        private static float ParseColumnFloat(HtmlNode row, int index)
        {
            var text = ParseColumnText(row, index);

            return float.TryParse(text,NumberStyles.Any, new CultureInfo("de-DE"), out var result) ? result : float.NaN;
        }

        private static int ParseColumnInt(HtmlNode row, int index)
        {
            var text = ParseColumnText(row, index);

            return int.TryParse(text, out var result) ? result : 0;
        }

        private static ExamState ParseExamState(string str)
        {
            return str.ToLower() switch
            {
                "Prüfung vorhanden" => ExamState.ExamExists,
                "bestanden" => ExamState.Passed,
                "nicht bestanden" => ExamState.Failed,
                _ => ExamState.Unknown
            };
        }
    }
}