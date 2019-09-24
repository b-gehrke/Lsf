﻿using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ical.Net.Serialization;
using Lsf.Client;
using Lsf.Models;
using Lsf.Parser;

namespace Lsf
{
    internal class Program
    {
        private static void MainMenu()
        {
                        
        }
        
        private static void Main(string[] args)
        {
            Console.Write("Please enter the base url of the LSF (https://lsf.ovgu.de/qislsf): ");
            var input = Console.ReadLine();
            
            var baseUrl = string.IsNullOrEmpty(input) ? "https://lsf.ovgu.de/qislsf" : input;
            var builder = new ScheduleBuilder<CalSchedule, CalScheduleItem>(new CalEventParser(baseUrl));

            CalSchedule[] schedules = null;

            while (input != "5")
            {
                var actionAddEvent = "0";
                var actionPreferEarly = "1";
                var actionBuild = "2";
                var actionToIcal = "3";
                var actionToLsf = "4";
                var actionLoadFromFile = "5";
                var actionSaveFile = "6";
                var actionExit = "7";
                
                Console.WriteLine($"[{actionAddEvent}]: Add an event");
                Console.WriteLine($"[{actionPreferEarly}]: Prefer an early timetable");
                Console.WriteLine($"[{actionBuild}]: Build timetables");
                Console.WriteLine($"[{actionToIcal}]: Export timetable to ical");
                Console.WriteLine($"[{actionToLsf}]: Export timetable to lsf");
                Console.WriteLine($"[{actionLoadFromFile}]: Load previous configured events from file");
                Console.WriteLine($"[{actionSaveFile}]: Safe configured events to file");
                Console.WriteLine($"[{actionExit}]: Exit");

                Console.Write("Please enter the number of the action you want to perform: ");

                input = Console.ReadLine();

                switch (input)
                {
                    case actionAddEvent:
                    {
                        Console.Write("Please paste the url of the event: ");
                        input = Console.ReadLine();
                        var eventId = Regex.Match(input, @"publishid=(\d+)").Groups[1].Value;

                        builder.AddEvent(eventId);

                        Console.Write("Do you prefer a teacher for this event? [y/N]: ");
                        input = Console.ReadLine();

                        while (input.ToLower() == "y")
                        {
                            Console.Write("Name of the person as written in the LSF: ");
                            input = Console.ReadLine();

                            var person = input;
                            builder.AddItemCriterion(new TeacherCriterion(person, true, eventId));

                            Console.Write("Prefer another one as well? [y/N]: ");
                            input = Console.ReadLine();
                        }

                        Console.Write("Do you want to avoid a teacher? [y/N]: ");
                        input = Console.ReadLine();
                        while (input.ToLower() == "y")
                        {
                            Console.Write("Name of the person as written in the LSF: ");
                            input = Console.ReadLine();

                            var person = input;
                            builder.AddItemCriterion(new TeacherCriterion(person, false, eventId));

                            Console.Write("Avoid another one as well? [y/N]: ");
                            input = Console.ReadLine();

                        }

                        break;
                    }

                    case actionPreferEarly:
                    {
                        builder.AddItemCriterion(new EarlyCriterion());
                        Console.WriteLine("I'll keep that in mind.");

                        break;
                    }

                    case actionBuild:
                    {
                        schedules = builder.Build().Result;
                        break;
                    }

                    case actionToIcal:
                    {
                        if (schedules is null)
                        {
                            Console.WriteLine("Please build the schedules first.");
                            break;
                        }

                        Console.Write(
                            "How many schedules (sorted by rating) to you want to export? Leaving this empty will export only the top rated one. [1-" +
                            schedules.Length + "] ");
                        input = Console.ReadLine();

                        var amount = 1;
                        if (string.IsNullOrEmpty(input) ||
                            int.TryParse(input, out amount) && amount <= schedules.Length)
                        {
                            Console.Write("Please enter the output folder: ");
                            var folder = Console.ReadLine();

                            for (var i = 0; i < amount; i++)
                            {
                                var schedule = schedules[i];
                                var serializer = new CalendarSerializer(schedule.ToCalendar());
                                var ical = serializer.SerializeToString();
                                File.WriteAllText(Path.Combine(folder, "schedule-" + i + ".ical"), ical);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid input");
                        }

                        break;
                    }

                    case actionToLsf:
                    {
                        if (schedules is null)
                        {
                            Console.WriteLine("Please build the schedules first.");
                            break;
                        }

                        Console.Write("Which schedule? (Defaults to 1) [1-" + schedules.Length + "] ");
                        input = Console.ReadLine();

                        var i = 1;
                        if (string.IsNullOrEmpty(input) ||
                            int.TryParse(input, out i) && i <= schedules.Length)
                        {
                            Console.Write("Please enter your lsf username: ");
                            var userName = Console.ReadLine();

                            Console.Write("Please enter your lsf password: ");
                            var password = Console.ReadLine();

                            var client = new LsfClient("https://lsf.ovgu.de");
                            var success = client.Authenticate(userName, password).Result;

                            var top = schedules[i];

                            if (!success)
                            {
                                Console.WriteLine("Authentication failed!");
                            }
                            else
                            {
                                client.SetSemester(2019, SemesterType.Winter).Wait();
                                client.ReplaceSchedule(top.ScheduleItems.Select(x => x.ScheduleComponent).ToArray())
                                    .Wait();
                                Console.WriteLine("Saved best schedule to lsf account!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid input");
                        }

                        break;
                    }
                }
            }
        }
    }
}