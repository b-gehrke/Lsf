using Lsf.Grading.Models;

namespace Lsf.Grading.Services
{
    public record ExamResultChangeTracking
    {
        public string DegreeName { get; init; }
        public string DegreeId { get; init; }
        public string MajorName { get; init; }
        public string MajorId { get; init; }
        public ExamResult ExamResult { get; init; }
    }
}