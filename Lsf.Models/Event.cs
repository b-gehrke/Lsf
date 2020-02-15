namespace Lsf.Models
{
    public class Event : IWebScheduleComponent
    {
        public string EventId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Appointment[] Appointments { get; set; }
        public SmallGroup[] SmallGroups { get; set; }
        public bool HasSmallGroup => SmallGroups != null;

        public string ScheduleId { get; set; } = null;
    }
}