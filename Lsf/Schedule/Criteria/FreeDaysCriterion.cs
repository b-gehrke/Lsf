using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public class FreeDaysCriterion : ScheduleCriterion
    {
        public FreeDaysCriterion(double weight = 1): base(false, null, weight)
        {
        }

        public override double Rate(ISchedule schedule)
        {
            return 1 - schedule.ScheduleItems.GroupBy(s => s.Appointment.DayOfWeek).Count() / 6d;
        }

        public override bool AppliesTo(ISchedule schedule)
        {
            return true;
        }
        
        public override bool Equals(object obj)
        {
            return obj is FreeDaysCriterion;
        }

        public override int GetHashCode()
        {
            return (int) (Weight * 100) + nameof(FreeDaysCriterion).GetHashCode();
        }
    }
}