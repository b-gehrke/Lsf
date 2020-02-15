using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lsf.Models
{
    public class Schedule<T> : ISchedule<T> where T: ScheduleItem
    {
        public T[] ScheduleItems { get; set; }
        ScheduleItem[] ISchedule.ScheduleItems
        {
            get => (T[])(object)ScheduleItems;
            set => ScheduleItems = (T[]) value;
        }

        public double Rating { get; set; } = 0;

        public IEnumerator<T> GetEnumerator()
        {
            return new List<T>(ScheduleItems).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Valid()
        {
            return ScheduleItems.All(a => ScheduleItems.All(b => a == b || !a.Appointment.ConflictsWith(b.Appointment)));
        }
    }
}