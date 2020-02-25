using System.Collections.Generic;
using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public abstract class ItemCriterion : IItemCriterion
    {
        protected ItemCriterion(bool multipleCriteriaAllowed = true, IEnumerable<ICriterion> excludes = null, double weight = 1)
        {
            MultipleCriteriaAllowed = multipleCriteriaAllowed;
            Weight = weight;
        }

        public bool MultipleCriteriaAllowed { get; }
        public virtual bool ConflictsWith(ICriterion other)
        {
            return false;
        }

        public double Weight { get; }
        public abstract double Rate(ScheduleItem item);

        public abstract bool AppliesTo(ScheduleItem item);
    }
}