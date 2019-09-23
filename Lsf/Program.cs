using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ical.Net.Serialization;
using Lsf.Client;
using Lsf.Models;
using Lsf.Parser;

namespace Lsf
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseUrl = "https://lsf.ovgu.de/qislsf";
//            var events = new[] {"145562", "147274", "144876", "145581"};

            var builder = new ScheduleBuilder<CalSchedule, CalScheduleItem>(new CalEventParser(baseUrl));
            builder.AddItemCriterion(new TeacherCriterion("Höding", true, "144953", .8));
            builder.AddItemCriterion(new TeacherCriterion("Reichel", false, "145581", 20));
            builder.AddItemCriterion(new TeacherCriterion("Neuhaus", true, "145581", 20));
            builder.AddItemCriterion(new EarlyCriterion());

//            foreach (var @event in events) builder.AddEvent(@event);

            builder.AddEvent("144876"); // TheoInf V 
            builder.AddEvent("145581"); // TheoInf Ü
//            
            builder.AddEvent("147497"); // IS Ü
            builder.AddEvent("147124"); // IS V 
//             
            builder.AddEvent("146439"); // IT-PM V 
            builder.AddEvent("146692"); // IT-PM Ü 
             
            builder.AddEvent("144952"); // Mathe 3 V 
            builder.AddEvent("144953"); // Mathe 3 Ü
            
            builder.AddEvent("145562"); // TI i Ü
             
//            builder.AddEvent("145003"); // Hardwarenahe Rechnerarchitektur Ü  
//            builder.AddEvent("146376"); // Hardwarenahe Rechnerarchitektur V
//            builder.AddEvent("146538"); // Prinzipien und Komponenten eingebetteter Systeme

            builder.AddEvent("144875"); // EinfInf T
//             
            builder.AddEvent("147524"); // Hot Topics in Communication and Networked Systems S
            
            builder.AddEvent("146947"); // Machine Learning V
            builder.AddEvent("146001"); // Machine Learning Ü

            var schedules = builder.Build().Result;

            var top = schedules.FirstOrDefault();
            if (top != null)
            {
//                var serializer = new CalendarSerializer(top.ToCalendar());
//                var ical = serializer.SerializeToString();
//                File.WriteAllText("schedule.ical", ical);
//                Process.Start("/usr/bin/calendar", "-f schedule.ical");
                var client = new LsfClient("https://lsf.ovgu.de");
                var success = client.Authenticate("usr", "pwd").Result;

                if (!success)
                {
                    Console.WriteLine("Authentication failed!");
                }
                else
                {
                    client.SetSemester(2019, SemesterType.Winter).Wait();
                    client.ReplaceSchedule(top.ScheduleItems.Select(x => x.ScheduleComponent).ToArray()).Wait();
                    Console.WriteLine("Saved best schedule to lsf account!");
                }
            }

            Console.WriteLine(schedules);
        }
    }
}