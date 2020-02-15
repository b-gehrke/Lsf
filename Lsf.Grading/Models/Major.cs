using System;
using System.Collections;
using System.Collections.Generic;
using Lsf.Models;

namespace Lsf.Grading.Models
{
    public class Major
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public IEnumerable<ExamResult> Gradings { get; set; }
    }

    public class ExamResult
    {
        public string Name { get; set; }
        public string ExamNumber { get; set; }
        public Semester Semester { get; set; }
        public int Try { get; set; }
        public DateTime Date { get; set; }
        public float Grade { get; set; }
        public ExamState ExamState { get; set; }
    }

    public enum ExamState
    {
        ExamExists,
        Passed,
        Failed,
        Unknown
    }
}