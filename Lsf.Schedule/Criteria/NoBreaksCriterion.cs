using System;
using System.Collections.Generic;
using System.Linq;
using Lsf.Models;

namespace Lsf.Schedule.Criteria
{
    public class NoBreaksCriterion : ScheduleCriterion
    {
        public override double Rate(ISchedule schedule)
        {
            var ordered = schedule.ScheduleItems.OrderBy(x => x.Appointment.Start).ToArray();
            var rating = 0f;
            var total = 0;

            for (var i = 0; i < ordered.Length - 1; i++)
            {
                var current = ordered[i];

                var next = ordered
                    .Skip(i)
                    .SkipWhile(x =>
                        x.Appointment.Recurring != current.Appointment.Recurring &&
                        x.Appointment.DayOfWeek == current.Appointment.DayOfWeek)
                    .FirstOrDefault(x =>
                        x.Appointment.DayOfWeek == current.Appointment.DayOfWeek);
                    
                if (next != null)
                {
                    var diff = (current.Appointment.End - next.Appointment.Start).Minutes;
                    rating += diff == 0 ? 1 : 0;
                    total += 1;
                }
            }

            return rating / total;
        }

        public override bool AppliesTo(ISchedule schedule)
        {
            return true;
        }
    }
}