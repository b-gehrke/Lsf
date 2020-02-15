using System;

namespace Lsf.Models
{
    public class Appointment
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Room { get; set; }
        public string Person { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public string EventId { get; set; }
        public RecurringType Recurring { get; set; }


        public bool ConflictsWith(Appointment other)
        {
            return Conflict(this, other);
        }

        public static bool Conflict(Appointment a, Appointment b)
        {
            if (a is null || b is null) return false;

            return a.DayOfWeek == b.DayOfWeek && (a.Recurring == RecurringType.Weekly ||
                                                  b.Recurring == RecurringType.Weekly || a.Recurring == b.Recurring) &&
                   !(a.End <= b.Start || b.End <= a.Start);
        }
    }
}