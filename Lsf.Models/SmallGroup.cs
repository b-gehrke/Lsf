namespace Lsf.Models
{
    public class SmallGroup : IWebScheduleComponent
    {
        public string Name { get; set; }
        public Appointment[] Appointments { get; set; }
        public string ScheduleId { get; set; }
        public string EventId { get; set; }
    }
}