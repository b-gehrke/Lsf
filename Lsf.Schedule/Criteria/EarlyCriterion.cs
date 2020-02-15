using Lsf.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lsf.Schedule.Criteria
{
    public class EarlyCriterion : ItemCriterion
    {
        [JsonProperty("minMinutes")]
        private readonly int _minMinutes;

        public EarlyCriterion(int minMinutes = 7 * 60) : base(false)
        {
            _minMinutes = minMinutes;
        }

        public override double Rate(ScheduleItem schedule)
        {
            return (double)_minMinutes / (schedule.Appointment.Start.Hour * 60 + schedule.Appointment.Start.Minute);
        }

        public override bool AppliesTo(ScheduleItem schedule)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is EarlyCriterion criterion && criterion._minMinutes == _minMinutes;
        }

        public override int GetHashCode()
        {
            return _minMinutes * 100 + (int)(Weight * 100) + nameof(EarlyCriterion).GetHashCode();
        }
    }
}