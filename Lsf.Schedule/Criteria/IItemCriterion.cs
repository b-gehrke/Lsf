using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public interface IItemCriterion : IWeightedCriterion
    {
        double Rate(ScheduleItem schedule);        
        bool AppliesTo(ScheduleItem schedule);

    }
}