using System.Collections;
using System.Collections.Generic;

namespace Lsf.Models
{
    public interface ISchedule : IEnumerable
    {
        ScheduleItem[] ScheduleItems { get; set; }
        double Rating { get; set; }
        bool Valid();        
    }
    
    public interface ISchedule<T> : ISchedule, IEnumerable<T> where T:ScheduleItem
    {
    }
}