using System;
using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public class EquallyDistributedAppointmentsPerDayCriterion : ScheduleCriterion
    {
        public EquallyDistributedAppointmentsPerDayCriterion(double weight = 1) : base(false, null, weight)
        {
        }

        public override bool AppliesTo(ISchedule schedule)
        {
            return true;
        }

        public override double Rate(ISchedule schedule)
        {
            var appointmentsPerDay = schedule.ScheduleItems.GroupBy(s => s.Appointment.DayOfWeek, s => s, (dayOfWeek, items) => (dayOfWeek: dayOfWeek, count: items.Count())).ToArray();
            var average = appointmentsPerDay.Average(x => x.count);
            var max = appointmentsPerDay.Max(x => x.count);
            
            
            return 1 - appointmentsPerDay
                       .Select(x => Math.Abs(x.count - average) / max).Sum() / appointmentsPerDay.Length;
        }
    }
}