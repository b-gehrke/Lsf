using Lsf.Models;

namespace Lsf
{
    public interface IWeightedCriterion
    {
        double Weight { get; }
    }
    
    public interface IScheduleCriterion : IWeightedCriterion
    {
        double Rate(ISchedule schedule);
        bool AppliesTo(ISchedule schedule);
    }

    public interface IItemCriterion : IWeightedCriterion
    {
        double Rate(ScheduleItem schedule);        
        bool AppliesTo(ScheduleItem schedule);

    }

    public class TeacherCriterion : IItemCriterion
    {
        private readonly string _person;
        private readonly bool _prefer;
        private string _eventId;

        public TeacherCriterion(string person, bool prefer, string eventId, double weight = 1)
        {
            _person = person;
            _prefer = prefer;
            _eventId = eventId;
            Weight = weight;
        }

        public double Weight { get; } 
        public double Rate(ScheduleItem schedule)
        {
            return schedule.Appointment.Person == _person && _prefer ? 0 : 1;
        }

        public bool AppliesTo(ScheduleItem schedule)
        {
            return schedule.ScheduleComponent.EventId == _eventId;
        }
    }
}