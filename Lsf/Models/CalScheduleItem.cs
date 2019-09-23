using Ical.Net.CalendarComponents;

namespace Lsf.Models
{
    public class CalScheduleItem : ScheduleItem
    {
        public CalendarEvent CalendarEvent => (Appointment as CalAppointment)?.CalendarEvent;
    }
}