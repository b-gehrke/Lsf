using System;
using System.IO;
using System.Text.RegularExpressions;
using Ical.Net;

namespace Lsf.Util
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var source = File.ReadAllText("/home/bjoern/Downloads/cal.ics");
            var cleanedSource = Regex.Replace(Regex.Replace(source.Replace("\r", "\n"), "\\n+", "\n"), "\\n(?:([^A-Z]))", "$1");
            var cal = Calendar.Load(cleanedSource);
            
            Console.WriteLine(CalendarPrinter.CalendarToFormattedString(cal, Console.BufferWidth));
        }
    }
}