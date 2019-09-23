using Ical.Net.CalendarComponents;

namespace Lsf.Models
{
    public class CalAppointment : Appointment
    {
        public CalendarEvent CalendarEvent { get; set; }
    }
}