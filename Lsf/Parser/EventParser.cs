using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Lsf.Models;
using Calendar = Ical.Net.Calendar;

namespace Lsf.Parser
{
    public class EventParser : WebParser
    {
        public EventParser(string baseUrl) : base(baseUrl)
        {
        }

        private RecurringType RecurringTypeFromCalendarEvent(CalendarEvent @event)
        {
            var rule = @event.RecurrenceRules.SingleOrDefault(r => r.Frequency == FrequencyType.Weekly);

            if (rule is null) return RecurringType.Single;

            return rule.Interval != 2 ? RecurringType.Weekly :
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(@event.Start.Date,
                    CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) % 2 == 0 ? RecurringType.EvenWeeks :
                RecurringType.OddWeeks;
        }

        protected virtual Appointment ParseAppointment(CalendarEvent ev, string eventId)
        {
            var person = Regex.Match(ev.Summary, @".*\)? \((\w+)\)").Groups[1].Value;

            return new Appointment
            {
                Start = new DateTime(1,1,1, ev.Start.Hour, ev.Start.Minute, 0),
                End = new DateTime(1,1,1, ev.End.Hour, ev.End.Minute, 0),
                Person = person,
                Recurring = RecurringTypeFromCalendarEvent(ev),
                Room = ev.Location,
                DayOfWeek = DayOfWeekFromEvent(ev),
                EventId = eventId
            };
        }

        private DayOfWeek DayOfWeekFromEvent(CalendarEvent ev)
        {
            var rule = ev.RecurrenceRules.SingleOrDefault(r => r.Frequency == FrequencyType.Weekly);

            return rule?.ByDay[0].DayOfWeek ?? ev.Start.DayOfWeek;
        }

        public async Task<Event> Parse(string eventId)
        {
            var url =
                $"{BaseUrl}/rds?state=verpublish&status=init&vmfile=no&publishid={eventId}&moduleCall=webInfo&publishConfFile=webInfo&publishSubDir=veranstaltung";
            var document = await GetHtmlAsync(url);

            var icalsImage = document.DocumentNode.QuerySelectorAll("caption a > img")
                .Where(x => x.Attributes["title"]?.Value == "iCalendar Export");
            var links = icalsImage.Select(image => image.ParentNode.GetAttributeValue("href", null))
                .Where(x => x != null)
                .Select(HtmlEntity.DeEntitize);

            var groups = (await Task.WhenAll(links.Select(GetAsync)))
                .Select(content => Regex.Replace(Regex.Replace(content.Replace("\r", "\n"), "\\n+", "\n"), "\\n(?:([^A-Z]))", "$1"))
                .Select(Calendar.Load)
                .Select((ical, i) => (events: ical.Events.Select(e => ParseAppointment(e, eventId)).ToArray(),
                    group: i + 1,
                    native: ical.Events))
                .ToArray();

            if (groups.Length > 0)
            {
                var native = groups[0].native[0];

                if (groups.Length == 1)
                    return new Event
                    {
                        Appointments = groups.First().events,
                        Name = native.Summary,
                        Type = native.Categories.FirstOrDefault(),
                        EventId = eventId
                    };

                return new Event
                {
                    Name = native.Summary,
                    Type = native.Categories.FirstOrDefault(),
                    EventId = eventId,
                    SmallGroups = groups.Select(ev => new SmallGroup
                    {
                        Appointments = ev.events,
                        Name = ev.group.ToString(),
                        ScheduleId = ev.group.ToString(),
                        EventId = eventId
                        
                    }).ToArray()
                };
            }

            return null;
        }
    }
}