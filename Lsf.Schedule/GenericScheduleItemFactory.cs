using System;
using Lsf.Models;

namespace Lsf.Schedule
{
    public class GenericScheduleItemFactory<TItem> : IScheduleItemFactory<TItem> where TItem : ScheduleItem
    {
        private readonly Func<Appointment, IWebScheduleComponent, TItem> _create;

        public GenericScheduleItemFactory(Func<Appointment, IWebScheduleComponent, TItem> create)
        {
            _create = create;
        }

        public TItem Create(Appointment appointment, IWebScheduleComponent scheduleComponent)
        {
            return _create(appointment, scheduleComponent);
        }
    }
}