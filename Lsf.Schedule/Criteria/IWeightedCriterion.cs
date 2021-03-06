using System.Collections;
using System.Collections.Generic;

namespace Lsf.Schedule.Criteria
{
    public interface IWeightedCriterion : ICriterion
    {
        double Weight { get; }
    }

    public interface ICriterion
    {
        /// <summary>
        /// Whether or not multiple criteria of this kind are allowed to be applied at the same time
        /// </summary>
        bool MultipleCriteriaAllowed { get; }

        /// <summary>
        /// Returns whether or not the conflicts with another criterion and therefore cannot be applied along with this criterion. E.g. because they mean the opposite.
        /// </summary>
        bool ConflictsWith(ICriterion other);
    }
}