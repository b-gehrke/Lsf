namespace Lsf.Models
{
    public class ScheduleItem
    {
        public Appointment Appointment { get; set; }
        public IWebScheduleComponent ScheduleComponent { get; set; }
    }
}