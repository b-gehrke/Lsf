using System;
using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public class SameAlternatingWeekCriterion : ScheduleCriterion
    {
        public SameAlternatingWeekCriterion(double weight = 1): base(false, null, weight)
        {
        }

        public override double Rate(ISchedule schedule)
        {
            var alternatingAppointments = schedule.ScheduleItems.Where(s =>
                s.Appointment.Recurring == RecurringType.EvenWeeks ||
                s.Appointment.Recurring == RecurringType.OddWeeks).ToArray();

            return Math.Abs(alternatingAppointments.Sum(item =>
                       item.Appointment.Recurring == RecurringType.EvenWeeks ? 1 : -1)) /
                   (double) alternatingAppointments.Length;
        }

        public override bool AppliesTo(ISchedule schedule)
        {
            return true;
        }
        
        public override bool Equals(object obj)
        {
            return obj is SameAlternatingWeekCriterion;
        }

        public override int GetHashCode()
        {
            return (int) (Weight * 100) + nameof(SameAlternatingWeekCriterion).GetHashCode();
        }
    }
}