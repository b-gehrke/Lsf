using System.Collections.Generic;
using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public abstract class ScheduleCriterion : IScheduleCriterion
    {
        protected ScheduleCriterion(bool multipleCriteriaAllowed = true, IEnumerable<ICriterion> excludes = null, double weight = 1)
        {
            MultipleCriteriaAllowed = multipleCriteriaAllowed;
            Excludes = excludes ?? Enumerable.Empty<ICriterion>();
            Weight = weight;
        }

        public bool MultipleCriteriaAllowed { get; }
        public IEnumerable<ICriterion> Excludes { get; }
        public double Weight { get; }
        public abstract double Rate(ISchedule schedule);

        public abstract bool AppliesTo(ISchedule schedule);
    }
}