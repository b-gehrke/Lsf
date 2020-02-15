using Ical.Net.CalendarComponents;
using Lsf.Models;

namespace Lsf.Schedule.Models
{
    public class CalScheduleItem : ScheduleItem
    {
        public CalendarEvent CalendarEvent => (Appointment as CalAppointment)?.CalendarEvent;
    }
}