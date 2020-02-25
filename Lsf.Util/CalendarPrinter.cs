using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Lsf.Models;
using Calendar = Ical.Net.Calendar;

namespace Lsf.Util
{
    public class CalendarPrinter
    {
        private readonly int _maxWidth;
        private readonly FormattingStyle _formattingStyle;
        private readonly Calendar _calendar;


        public static string CalendarToFormattedString(Calendar calendar, int maxWidth)
        {
            return new CalendarPrinter(maxWidth, FormattingStyle.Console, calendar).Print();
        }

        public CalendarPrinter(int maxWidth, FormattingStyle formattingStyle, Calendar calendar)
        {
            _maxWidth = maxWidth;
            _formattingStyle = formattingStyle;
            _calendar = calendar;
        }

        private static string WithLineBreaks(string str, int lineWidth)
        {
            return string.Join("\n",
                Enumerable
                    .Range(0, str.Length / lineWidth)
                    .Select(i => str.Substring(i, Math.Min(lineWidth, str.Length - i * lineWidth))));
        }

        public string EventToString(IRecurringComponent calendarEvent, int width = int.MaxValue)
        {
            var actualWidth = Math.Min(new[] {calendarEvent.Name.Length, calendarEvent.Description.Length}.Max(),
                width);

            return $@"{WithLineBreaks(calendarEvent.Name, width)}
{"".PadLeft(actualWidth, '-')}
{WithLineBreaks(calendarEvent.Description, width)}
{"".PadLeft(actualWidth, '-')}

";
        }

        private static string Center(string str, int width, char paddingChar = ' ')
        {
            return string.Join("\n",
                str.Split("\n").Select(x =>
                    x.PadLeft(width / 2 + str.Length / 2 + 1, paddingChar).PadRight(width, paddingChar)));
        }

        public string Print()
        {
            var result = new StringBuilder();

            const int timeColumnWidth = 8;
            const int slotsPerHour = 4; // print 15 minutes slots
            var weekDays = _calendar.Events.Select(DayOfWeekFromEvent).Distinct().OrderBy(x => x).ToArray();
            var daysCount = weekDays.Length;
            var columnWidth = (_maxWidth - 1 - daysCount - timeColumnWidth - 1) / daysCount;

            var eventStrings = _calendar.Events.Select(e => EventToString(e, columnWidth));

            result.AppendLine("".PadLeft(_maxWidth, '='));
            result.Append("|");
            result.Append(Center("Time", timeColumnWidth));
            result.Append("|");

            foreach (var day in weekDays)
            {
                result.Append(Center(day.ToString(), columnWidth));
                result.Append("|");
            }

            result.AppendLine();

            result.AppendLine("".PadLeft(_maxWidth, '='));

            var earliestStartTime = _calendar.Events.Min(x => x.Start.Hour);
            var latestEndTime = _calendar.Events.Max(x => x.End.Hour) + 1;
            var startTimes = _calendar.Events.OrderBy(DayOfWeekFromEvent).GroupBy(x => x.Start.Hour)
                .OrderBy(x => x.Key);

            var matrix = new List<PrintingCalendarEvent>[daysCount, (latestEndTime - earliestStartTime) * slotsPerHour];

            foreach (var calendarEvent in _calendar.Events.Select(x => new PrintingCalendarEvent(x)))
            {
                var x = DayOfWeek(DayOfWeekFromEvent(calendarEvent));

                for (var i = 0;
                    i < (calendarEvent.End.Hour * slotsPerHour + calendarEvent.End.Minute / (60 / slotsPerHour)) -
                    (calendarEvent.Start.Hour * slotsPerHour + calendarEvent.Start.Minute /
                        (60 / slotsPerHour));
                    i++)
                {
                    var y = (calendarEvent.Start.Hour - earliestStartTime) * slotsPerHour +
                            calendarEvent.Start.Minute / (60 / slotsPerHour) + i;

                    matrix[x, y] = matrix[x, y] ?? new List<PrintingCalendarEvent>();
                    matrix[x, y].Add(calendarEvent);
                }
            }

            for (var hour = earliestStartTime; hour < latestEndTime; hour++)
            {
                for (int perHourOffset = 0; perHourOffset < slotsPerHour; perHourOffset++)
                {
                    result.Append("|");

                    result.Append(perHourOffset == 0
                        ? Center($"{hour}:00", timeColumnWidth)
                        : "".PadLeft(timeColumnWidth));

                    result.Append("|");

                    foreach (var day in weekDays)
                    {
                        var events = MatrixLookUp(day, hour, perHourOffset);
                        var entry = "";

                        if (events != null)
                        {
                            var availableSpacePerEvent = (columnWidth - (events.Count - 1)) / events.Count;

                            foreach (var calendarEvent in events)
                            {
                                var borderChar = RecurringTypeFromCalendarEvent(calendarEvent) switch
                                {
                                    RecurringType.Single => 'X',
                                    RecurringType.Weekly => '#',
                                    RecurringType.EvenWeeks => '0',
                                    RecurringType.OddWeeks => '%',
                                    _ => '@'
                                };
                                
                                if (events.IndexOf(calendarEvent) > 0)
                                {
                                    entry += ("|");
                                }

                                if (!calendarEvent.HeaderPrinted)
                                {
                                    entry += "".PadLeft(availableSpacePerEvent, borderChar);
                                    calendarEvent.HeaderPrinted = true;
                                }
                                else if (TryMatrixLookUp(day, perHourOffset == slotsPerHour - 1 ? hour + 1 : hour,
                                    perHourOffset == slotsPerHour - 1 ? 0 : perHourOffset + 1,
                                    out var nextEvents) && nextEvents?.Contains(calendarEvent) != true)
                                {
                                    entry += "".PadLeft(availableSpacePerEvent, borderChar) ;
                                }
                                else if (calendarEvent.Summary.Length > calendarEvent.CharactersPrinted)
                                {
                                    entry += borderChar + " ";
                                    entry += calendarEvent.Summary.MaxSubstring(
                                            calendarEvent.CharactersPrinted, availableSpacePerEvent - 4)
                                        .PadRight(availableSpacePerEvent - 4);
                                    calendarEvent.CharactersPrinted += availableSpacePerEvent - 4;
                                    entry += " " + borderChar;
                                }
                                else
                                {
                                    entry += borderChar + "".PadRight(availableSpacePerEvent-2) + borderChar;
                                }
                            }
                        }

                        result.Append(entry.PadRight(columnWidth));

                        result.Append("|");
                    }

                    result.AppendLine();
                }

                // result.AppendLine("".PadLeft(_maxWidth, '='));
            }

            var raw = result.ToString();

            return raw;


            List<PrintingCalendarEvent> MatrixLookUp(DayOfWeek dayOfWeek, int hour, int hourOffset = 0)
            {
                if (!TryMatrixLookUp(dayOfWeek, hour, hourOffset, out var events))
                {
                    throw new IndexOutOfRangeException();
                }

                return events;
            }

            bool TryMatrixLookUp(DayOfWeek dayOfWeek, int hour, int hourOffset, out List<PrintingCalendarEvent> events)
            {
                var x = DayOfWeek(dayOfWeek);
                var y = (hour - earliestStartTime) * slotsPerHour + hourOffset;

                if (x < matrix.GetLength(0) && y < matrix.GetLength(1))
                {
                    events = matrix[x, y];
                    return true;
                }

                events = null;
                return false;
            }
        }

        private static DayOfWeek DayOfWeekFromEvent(IRecurrable ev)
        {
            var rule = ev.RecurrenceRules.SingleOrDefault(r => r.Frequency == FrequencyType.Weekly);

            return rule?.ByDay[0].DayOfWeek ?? ev.Start.DayOfWeek;
        }

        private static int DayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                System.DayOfWeek.Sunday => 6,
                System.DayOfWeek.Monday => 0,
                System.DayOfWeek.Tuesday => 1,
                System.DayOfWeek.Wednesday => 2,
                System.DayOfWeek.Thursday => 3,
                System.DayOfWeek.Friday => 4,
                System.DayOfWeek.Saturday => 5,
                _ => -1
            };
        }
        
        private RecurringType RecurringTypeFromCalendarEvent(CalendarEvent @event)
        {
            var rule = @event.RecurrenceRules.SingleOrDefault(r => r.Frequency == FrequencyType.Weekly);

            if (rule is null) return RecurringType.Single;

            return rule.Interval != 2 ? RecurringType.Weekly :
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(@event.Start.Date,
                    CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday) % 2 == 0 ? RecurringType.EvenWeeks :
                RecurringType.OddWeeks;
        }


        private class PrintingCalendarEvent : CalendarEvent
        {
            private readonly CalendarEvent _calendarEvent = new CalendarEvent();
            public int CharactersPrinted { get; set; }

            public bool HeaderPrinted { get; set; }

            public PrintingCalendarEvent(CalendarEvent calendarEvent)
            {
                _calendarEvent = calendarEvent;
                CharactersPrinted = 0;
            }

            public override IDateTime DtStart
            {
                get => _calendarEvent.DtStart;
                set => _calendarEvent.DtStart = value;
            }

            public override IDateTime DtEnd
            {
                get => _calendarEvent.DtEnd;
                set => _calendarEvent.DtEnd = value;
            }

            public override TimeSpan Duration
            {
                get => _calendarEvent.Duration;
                set => _calendarEvent.Duration = value;
            }

            public override IDateTime End
            {
                get => _calendarEvent.End;
                set => _calendarEvent.End = value;
            }

            public override bool IsAllDay
            {
                get => _calendarEvent.IsAllDay;
                set => _calendarEvent.IsAllDay = value;
            }

            public override IList<string> Resources
            {
                get => _calendarEvent.Resources;
                set => _calendarEvent.Resources = value;
            }

            public override bool IsActive => _calendarEvent.IsActive;

            public override IList<Attachment> Attachments
            {
                get => _calendarEvent.Attachments;
                set => _calendarEvent.Attachments = value;
            }

            public override IList<string> Categories
            {
                get => _calendarEvent.Categories;
                set => _calendarEvent.Categories = value;
            }

            public override string Class
            {
                get => _calendarEvent.Class;
                set => _calendarEvent.Class = value;
            }

            public override IList<string> Contacts
            {
                get => _calendarEvent.Contacts;
                set => _calendarEvent.Contacts = value;
            }

            public override IDateTime Created
            {
                get => _calendarEvent.Created;
                set => _calendarEvent.Created = value;
            }

            public override string Description
            {
                get => _calendarEvent.Description;
                set => _calendarEvent.Description = value;
            }

            public override IList<PeriodList> ExceptionDates
            {
                get => _calendarEvent.ExceptionDates;
                set => _calendarEvent.ExceptionDates = value;
            }

            public override IList<RecurrencePattern> ExceptionRules
            {
                get => _calendarEvent.ExceptionRules;
                set => _calendarEvent.ExceptionRules = value;
            }

            public override IDateTime LastModified
            {
                get => _calendarEvent.LastModified;
                set => _calendarEvent.LastModified = value;
            }

            public override int Priority
            {
                get => _calendarEvent.Priority;
                set => _calendarEvent.Priority = value;
            }

            public override IList<PeriodList> RecurrenceDates
            {
                get => _calendarEvent.RecurrenceDates;
                set => _calendarEvent.RecurrenceDates = value;
            }

            public override IList<RecurrencePattern> RecurrenceRules
            {
                get => _calendarEvent.RecurrenceRules;
                set => _calendarEvent.RecurrenceRules = value;
            }

            public override IDateTime RecurrenceId
            {
                get => _calendarEvent.RecurrenceId;
                set => _calendarEvent.RecurrenceId = value;
            }

            public override IList<string> RelatedComponents
            {
                get => _calendarEvent.RelatedComponents;
                set => _calendarEvent.RelatedComponents = value;
            }

            public override int Sequence
            {
                get => _calendarEvent.Sequence;
                set => _calendarEvent.Sequence = value;
            }

            public override IDateTime Start
            {
                get => _calendarEvent.Start;
                set => _calendarEvent.Start = value;
            }

            public override string Summary
            {
                get => _calendarEvent.Summary;
                set => _calendarEvent.Summary = value;
            }

            public override ICalendarObjectList<Alarm> Alarms => _calendarEvent.Alarms;

            public override IList<Attendee> Attendees
            {
                get => _calendarEvent.Attendees;
                set => _calendarEvent.Attendees = value;
            }

            public override IList<string> Comments
            {
                get => _calendarEvent.Comments;
                set => _calendarEvent.Comments = value;
            }

            public override IDateTime DtStamp
            {
                get => _calendarEvent.DtStamp;
                set => _calendarEvent.DtStamp = value;
            }

            public override Organizer Organizer
            {
                get => _calendarEvent.Organizer;
                set => _calendarEvent.Organizer = value;
            }

            public override IList<RequestStatus> RequestStatuses
            {
                get => _calendarEvent.RequestStatuses;
                set => _calendarEvent.RequestStatuses = value;
            }

            public override Uri Url
            {
                get => _calendarEvent.Url;
                set => _calendarEvent.Url = value;
            }

            public override string Uid
            {
                get => _calendarEvent.Uid;
                set => _calendarEvent.Uid = value;
            }

            public override ICalendarObject Parent
            {
                get => _calendarEvent.Parent;
                set => _calendarEvent.Parent = value;
            }

            public override ICalendarObjectList<ICalendarObject> Children => _calendarEvent.Children;

            public override string Name
            {
                get => _calendarEvent.Name;
                set => _calendarEvent.Name = value;
            }

            public override int Line
            {
                get => _calendarEvent.Line;
                set => _calendarEvent.Line = value;
            }

            public override int Column
            {
                get => _calendarEvent.Column;
                set => _calendarEvent.Column = value;
            }

            public override string Group
            {
                get => _calendarEvent.Group;
                set => _calendarEvent.Group = value;
            }

            public override bool IsLoaded => _calendarEvent.IsLoaded;
        }
    }

    public enum FormattingStyle
    {
        Plain,
        Markdown,
        Console,
        Html
    }
}