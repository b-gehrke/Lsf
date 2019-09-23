using Ical.Net.CalendarComponents;
using Lsf.Models;

namespace Lsf.Parser
{
    public class CalEventParser : EventParser
    {
        public CalEventParser(string baseUrl) : base(baseUrl)
        {
        }

        protected override Appointment ParseAppointment(CalendarEvent ev, string eventId)
        {
            var baseResult = base.ParseAppointment(ev, eventId);
            
            return new CalAppointment
            {
                End = baseResult.End,
                Person = baseResult.Person,
                Recurring = baseResult.Recurring,
                Room = baseResult.Room,
                Start = baseResult.Start,
                CalendarEvent = ev,
                EventId = baseResult.EventId,
                DayOfWeek = baseResult.DayOfWeek
            };
        }
    }
}