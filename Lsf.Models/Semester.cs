using System;

namespace Lsf.Models
{
    public class Semester
    {
        public Semester(int year, SemesterType semesterType)
        {
            Year = year;
            SemesterType = semesterType;
        }

        public int Year { get; set; }
        public SemesterType SemesterType { get; set; }

        public static Semester Current
        {
            get
            {
                var d = DateTime.Now.AddMonths(-4);
                return new Semester(d.Year, d.Month <= 6 ? SemesterType.Summer : SemesterType.Winter);
            }
        }

        public Semester Next()
        {
            return new Semester(SemesterType == SemesterType.Summer ? Year : Year + 1,
                SemesterType == SemesterType.Summer ? SemesterType.Winter : SemesterType.Summer);
        }

        public static Semester FromSemesterCode(int code)
        {
            var semesterType = (SemesterType) (code % 10);
            var year = code / 10;

            return new Semester(year, semesterType);
        }

        public static implicit operator Semester(int code)
        {
            return FromSemesterCode(code);
        }

        public static implicit operator int(Semester semester)
        {
            return semester.ToSemesterCode();
        }

        public static Semester Parse(string str)
        {
            if (int.TryParse(str, out var code)) return FromSemesterCode(code);

            var semesterTypeStr = str.Substring(0, 4).ToLower();
            var yearStr = str.Substring(5, 2);

            SemesterType semesterType;
            if (semesterTypeStr == "wise")
                semesterType = SemesterType.Winter;
            else if (semesterTypeStr == "sose")
                semesterType = SemesterType.Summer;
            else
                throw new FormatException("The semester type string could not be parsed");

            if (int.TryParse(yearStr, out var year)) return new Semester(year, semesterType);

            throw new FormatException("The semester Year could not be parsed");
        }

        public int ToSemesterCode()
        {
            return Year * 10 + (int) SemesterType;
        }

        public override string ToString()
        {
            return (SemesterType == SemesterType.Summer ? "SoSe" : "WiSe") + " " + Year;
        }
    }
}