using Ical.Net.CalendarComponents;
using Lsf.Models;
using Lsf.Schedule.Models;

namespace Lsf.Parser
{
    public class CalEventParser : EventParser
    {
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