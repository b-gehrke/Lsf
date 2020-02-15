using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public interface IScheduleCriterion : IWeightedCriterion
    {
        double Rate(ISchedule schedule);
        bool AppliesTo(ISchedule schedule);
    }
}