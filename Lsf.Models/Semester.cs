using System;
using System.Security.Cryptography;

namespace Lsf.Models
{
    public class Semester
    {
        public int Year { get; set; }
        public SemesterType SemesterType { get; set; }

        public Semester(int year, SemesterType semesterType)
        {
            Year = year;
            SemesterType = semesterType;
        }

        public static Semester FromSemesterCode(int code)
        {
            var semesterType = (SemesterType) (code % 10);
            var year = code / 10;

            return new Semester(year, semesterType);
        }

        public static implicit operator Semester(int code) => FromSemesterCode(code);
        public static implicit operator int(Semester semester) => semester.ToSemesterCode();

        public static Semester Parse(string str)
        {
            var semesterTypeStr = str.Substring(0, 4).ToLower();
            var yearStr = str.Substring(5, 2);

            SemesterType semesterType;
            if (semesterTypeStr == "wise")
            {
                semesterType = SemesterType.Winter;
            } else if (semesterTypeStr == "sose")
            {
                semesterType = SemesterType.Summer;
            }
            else
            {
                throw new FormatException("The semester type string could not be parsed");
            }

            if (int.TryParse(yearStr, out var year))
            {
                return new Semester(year, semesterType);
            }
            
            throw new FormatException("The semester Year could not be parsed");
        }

        public int ToSemesterCode()
        {
            return Year * 10 + (int) SemesterType;
        }

        public override string ToString()
        {
            return ToSemesterCode().ToString();
        }
    }
}