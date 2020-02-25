namespace Lsf.Models
{
    public class Event : IWebScheduleComponent
    {
        public string EventId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public virtual Appointment[] Appointments { get; set; }
        public virtual SmallGroup[] SmallGroups { get; set; }
        public virtual bool HasSmallGroup => SmallGroups != null;

        public string ScheduleId { get; set; } = null;
    }
}