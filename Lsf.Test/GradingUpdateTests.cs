using System;
using Lsf.Grading.Models;
using Lsf.Grading.Services;
using Lsf.Models;
using NUnit.Framework;

namespace Lsf.Test
{
    /*
     * new ExamResult
                                {
                                    Date = DateTime.Now, Grade = 1f, Name = "TestGrad1", Semester = Semester.Current,
                                    Try = 1, ExamNumber = "1", ExamState = ExamState.Passed
                                }
     */

    public class GradingUpdateTests
    {
        [Test]
        public void UpdatedGradesTest()
        {
            var exam1 = new ExamResultChangeTracking
            {
                DegreeId = "1", DegreeName = "TestDegree1", MajorId = "1.1", MajorName = "MajorName1",
                ExamResult = new ExamResult
                {
                    Date = DateTime.MinValue, Grade = 1f, Name = "Exam1", Semester = Semester.Current, Try = 1,
                    ExamNumber = "1", ExamState = ExamState.Passed
                }
            };

            var exam2 = new ExamResultChangeTracking
            {
                DegreeId = "1", DegreeName = "TestDegree1", MajorId = "1.1", MajorName = "MajorName1",
                ExamResult = new ExamResult
                {
                    Date = DateTime.MinValue, Grade = 1f, Name = "Exam2", Semester = Semester.Current, Try = 1,
                    ExamNumber = "2", ExamState = ExamState.Passed
                }
            };

            var previousResults = new[]
            {
                exam1
            };
            var currentDegrees = new[]
            {
                exam1,
                exam2
            };

            var expected = new[] {exam2};
            var actual = Worker.GetChangedExams(previousResults, currentDegrees);

            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}