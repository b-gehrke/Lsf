using Lsf.Grading.Models;

namespace Lsf.Grading.Services
{
    public class ExamResultChangeTracking
    {
        public string DegreeName { get; set; }
        public string DegreeId { get; set; }
        public string MajorName { get; set; }
        public string MajorId { get; set; }
        public ExamResult ExamResult { get; set; }
    }
}