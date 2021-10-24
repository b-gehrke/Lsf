using System.Collections.Generic;
using System.Threading.Tasks;
using Lsf.Grading.Models;

namespace Lsf.Grading.Services.Notifiers
{
    /// <summary>
    /// Interface for classes that notify the user about changes in their grades
    /// </summary>
    public interface INotifier
    {
        /// <summary>
        /// Notifies users about changes in their grades
        /// </summary>
        /// <param name="degrees">Contains changed grades</param>
        Task NotifyChange(IEnumerable<Degree> degrees);

        
        /// <summary>
        /// Notifies users about an error
        /// </summary>
        /// <param name="message">Error message</param>
        Task NotifyError(string message);
    }
}