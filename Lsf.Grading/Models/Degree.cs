using System.Collections;
using System.Collections.Generic;

namespace Lsf.Grading.Models
{
    public class Degree
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public IEnumerable<Major> GradingMajors { get; set; }
    }
}