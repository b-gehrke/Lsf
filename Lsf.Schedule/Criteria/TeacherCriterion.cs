using Lsf.Models;
using Newtonsoft.Json;

namespace Lsf.Schedule.Criteria
{
    public class TeacherCriterion : ItemCriterion
    {
        [JsonProperty("person")]
        private readonly string _person;
        [JsonProperty("prefer")]
        private readonly bool _prefer;
        [JsonProperty("eventId")]
        private readonly string _eventId;

        public TeacherCriterion(string person, bool prefer, string eventId, double weight = 1) : base(true, null, weight)
        {
            _person = person;
            _prefer = prefer;
            _eventId = eventId;
        }
        
        public override double Rate(ScheduleItem schedule)
        {
            return schedule.Appointment.Person == _person && _prefer ? 0 : 1;
        }

        public override bool AppliesTo(ScheduleItem schedule)
        {
            return schedule.ScheduleComponent.EventId == _eventId;
        }
        
        public override bool Equals(object obj)
        {
            return obj is TeacherCriterion criterion && criterion._prefer == _prefer && criterion._person == _person && criterion._eventId == _eventId;
        }

        public override int GetHashCode()
        {
            return (int) (Weight * 100) + nameof(TeacherCriterion).GetHashCode();
        }
    }
}