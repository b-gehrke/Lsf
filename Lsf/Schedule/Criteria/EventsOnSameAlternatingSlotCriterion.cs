using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public class EventsOnSameAlternatingSlotCriterion : ScheduleCriterion
    {
        public EventsOnSameAlternatingSlotCriterion(double weight = 1): base(false, null, weight)
        {
        }

        public override double Rate(ISchedule schedule)
        {
            var alternatingAppointments = schedule.ScheduleItems.Where(s =>
                s.Appointment.Recurring == RecurringType.EvenWeeks ||
                s.Appointment.Recurring == RecurringType.OddWeeks).ToArray();

            return alternatingAppointments.Select(s => (s,
                           schedule.ScheduleItems.FirstOrDefault(ss =>
                               s.Appointment.DayOfWeek == ss.Appointment.DayOfWeek &&
                               s.Appointment.Start == ss.Appointment.Start)))
                       .Count(s => s.Item2 != null) / (double) alternatingAppointments.Length;
        }

        public override bool AppliesTo(ISchedule schedule)
        {
            return true;
        }
        
        public override bool Equals(object obj)
        {
            return obj is EventsOnSameAlternatingSlotCriterion;
        }

        public override int GetHashCode()
        {
            return (int) (Weight * 100) + nameof(EventsOnSameAlternatingSlotCriterion).GetHashCode();
        }
    }
}