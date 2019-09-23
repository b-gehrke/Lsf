using Lsf.Models;

namespace Lsf
{
    public class EarlyCriterion : IItemCriterion
    {
        private readonly int _minMinutes;

        public EarlyCriterion(int minMinutes = 7 * 60)
        {
            _minMinutes = minMinutes;
        }

        public double Weight { get; } = 1;
        public double Rate(ScheduleItem schedule)
        {
            return (double)_minMinutes / (schedule.Appointment.Start.Hour * 60 + schedule.Appointment.Start.Minute);
        }

        public bool AppliesTo(ScheduleItem schedule)
        {
            return true;
        }
    }
}