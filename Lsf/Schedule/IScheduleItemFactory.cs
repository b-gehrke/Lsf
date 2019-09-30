using Lsf.Models;

namespace Lsf.Schedule
{
    public interface IScheduleItemFactory<out TItem> where TItem: ScheduleItem
    {
        TItem Create(Appointment appointment, IWebScheduleComponent scheduleComponent);
    }
}