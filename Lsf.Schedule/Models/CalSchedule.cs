using Lsf.Models;
using Ical.Net;

namespace Lsf.Schedule.Models
{
    public class CalSchedule : Schedule<CalScheduleItem>
    {
        public Calendar ToCalendar()
        {
            var calendar = new Calendar();

            foreach (var item in ScheduleItems)
            {
                calendar.Events.Add(item.CalendarEvent);
            }

            return calendar;
        }
    }
}