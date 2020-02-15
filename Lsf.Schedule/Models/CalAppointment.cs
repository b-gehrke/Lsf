using Ical.Net.CalendarComponents;
using Lsf.Models;

namespace Lsf.Schedule.Models
{
    public class CalAppointment : Appointment
    {
        public CalendarEvent CalendarEvent { get; set; }
    }
}