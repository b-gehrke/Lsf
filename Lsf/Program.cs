using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ical.Net.Serialization;
using Lsf.Client;
using Lsf.Models;
using Lsf.Parser;
using Lsf.Schedule;
using Lsf.Schedule.Criteria;
using Lsf.Schedule.Models;
using Lsf.Util;

namespace Lsf
{
    internal class Program
    {
        private static void MainMenu()
        {
        }

        private static string ReadWithDefault(string message, string def)
        {
            Console.Write($"{message} ({def}): ");
            var input = Console.ReadLine();
            return string.IsNullOrEmpty(input) ? def : input;
        }

        private static void Main(string[] args)
        {
            string input = null;
            var baseUrl = ReadWithDefault("Please enter the base url of the LSF", "https://lsf.ovgu.de");

            var semesterStr = ReadWithDefault("Please enter a semester", Semester.Current.Next().ToString());
            var semester = Semester.Parse(semesterStr);

            var httpClient = new LsfHttpClientImpl(baseUrl);
            var client = new LsfScheduleClient(httpClient, new CalEventParser());
            var builder = new ScheduleBuilder<CalSchedule, CalScheduleItem>(client,
                new GenericScheduleItemFactory<CalScheduleItem>((appointment, component) => new CalScheduleItem
                {
                    Appointment = appointment,
                    ScheduleComponent = component
                }));

            CalSchedule[] schedules = null;


            var earlyCriterion = new EarlyCriterion();
            var equallyDistributedAppointmentsPerDayCriterion = new EquallyDistributedAppointmentsPerDayCriterion();
            var freeDaysCriterion = new FreeDaysCriterion();
            var eventsOnSameAlternatingSlotCriterion = new EventsOnSameAlternatingSlotCriterion(10000);
            var sameAlternatingWeekCriterion = new SameAlternatingWeekCriterion();
            var noBreaksCriterion = new NoBreaksCriterion();

            const string actionAddEvent = "0";
            const string addCriterion = "1";
            const string actionBuild = "2";
            const string actionToIcal = "3";
            const string actionToLsf = "4";
            const string actionPrintCal = "5";
            const string actionLoadFromFile = "6";
            const string actionSaveFile = "7";
            const string actionExit = "8";
            const string actionGetLoginCookie = "9";
            while (input != actionExit)
            {
//                Console.Clear();

                Console.WriteLine(
                    $"{builder.EventsCount} events saved. Timetables are {(builder.IsBuild ? "" : "not ")}build");
                Console.WriteLine($"[{actionAddEvent}]: Add an event");
                Console.WriteLine($"[{addCriterion}]: Configure a preferences");
                Console.WriteLine($"[{actionBuild}]: Build timetables");
                Console.WriteLine($"[{actionToIcal}]: Export timetable to ical");
                Console.WriteLine($"[{actionToLsf}]: Export timetable to lsf");
                Console.WriteLine($"[{actionPrintCal}]: Print timetable to console");
                Console.WriteLine($"[{actionLoadFromFile}]: Load previous configured events from file");
                Console.WriteLine($"[{actionSaveFile}]: Safe configured events to file");
                Console.WriteLine($"[{actionExit}]: Exit");

                input = Console.ReadLine();

                switch (input)
                {
                    case actionAddEvent:
                    {
                        Console.Write("Please paste the url of the event: ");
                        input = Console.ReadLine();
                        var eventId = Regex.Match(input, @"publishid=(\d+)").Groups[1].Value;

                        var e = builder.AddEvent(eventId);

                        Console.Write(
                            "Does the event consist of small groups and did you already got assigned (to one or more)? [y/N]: ");
                        input = Console.ReadLine();

                        if (input.ToLower() == "y")
                        {
                            while (input.ToLower() == "y")
                            {
                                Console.Write("GroupName (usually the group number): ");
                                var groupName = Console.ReadLine();
                                e.WithFixedSmallGroup(groupName);

                                Console.Write("Another one? [y/N]: ");
                                input = Console.ReadLine();
                            }

                            break;
                        }

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

                    case addCriterion:
                    {
                        void AddOrRemoveCriterion(ICriterion criterion, string message)
                        {
                            var currentState = builder.HasCriterion(criterion);

                            Console.Write($"{message} [{(currentState ? "Y/n" : "y/N")}]: ");
                            input = Console.ReadLine().ToLower();
                            if (input == "y" && !currentState)
                                switch (criterion)
                                {
                                    case IItemCriterion itemCriterion:
                                        builder.AddItemCriterion(itemCriterion);
                                        break;
                                    case IScheduleCriterion scheduleCriterion:
                                        builder.AddScheduleCriterion(scheduleCriterion);
                                        break;
                                }
                            else if (input == "n" && currentState)
                                switch (criterion)
                                {
                                    case IItemCriterion itemCriterion:
                                        builder.RemoveItemCriterion(itemCriterion);
                                        break;
                                    case IScheduleCriterion scheduleCriterion:
                                        builder.RemoveScheduleCriterion(scheduleCriterion);
                                        break;
                                }
                        }

                        AddOrRemoveCriterion(earlyCriterion, "Do you prefer an early timetable?");
                        AddOrRemoveCriterion(equallyDistributedAppointmentsPerDayCriterion,
                            "Do you prefer appointments equally distributed over every day?");
                        AddOrRemoveCriterion(freeDaysCriterion, "Do you prefer free days?");
                        AddOrRemoveCriterion(eventsOnSameAlternatingSlotCriterion,
                            "Do you prefer filling an empty 2-Weeks event slot with an other 2-Weeks event?");
                        AddOrRemoveCriterion(sameAlternatingWeekCriterion,
                            "Do you prefer all 2-Week events to be on the same week?");
                        AddOrRemoveCriterion(noBreaksCriterion, "Do you prefer no breaks between you lessons?");

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

                        if (schedules.Length == 0)
                        {
                            Console.WriteLine("There are no schedules to export.");
                            break;
                        }

                        Console.Write("Which schedule? (Defaults to 1) [1-" + schedules.Length + "] ");
                        input = Console.ReadLine();

                        var i = 1;
                        if (string.IsNullOrEmpty(input) ||
                            int.TryParse(input, out i) && i <= schedules.Length)
                        {
                            if (!httpClient.IsAuthenticated)
                                if (!Authenticate(httpClient))
                                    break;

                            var top = schedules[i];
                            httpClient.SetSemester(semester).Wait();
                            client.ReplaceSchedule(top.ScheduleItems.Select(x => x.ScheduleComponent).ToArray())
                                .Wait();
                            Console.WriteLine("Saved best schedule to lsf account!");
                        }
                        else
                        {
                            Console.WriteLine("Invalid input");
                        }

                        break;
                    }

                    case actionPrintCal:
                    {
                        Console.Write("Which schedule? (Defaults to 1) [1-" + schedules.Length + "] ");
                        input = Console.ReadLine();

                        var i = 1;
                        if (string.IsNullOrEmpty(input) ||
                            int.TryParse(input, out i) && i <= schedules.Length)
                        {
                            var top = schedules[i];
                            Console.WriteLine(new CalendarPrinter(Console.BufferWidth, FormattingStyle.Console,
                                top.ToCalendar()).Print());
                        }
                        else
                        {
                            Console.WriteLine("Invalid input");
                        }

                        break;
                    }

                    case actionSaveFile:
                    {
                        var path = Path.GetFullPath(Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lsf-api",
                            "events.json"));
                        Console.Write($"Where should the file be stored? [{path}]: ");
                        input = Console.ReadLine();

                        if (!string.IsNullOrEmpty(input)) path = Path.GetFullPath(input);

                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        var state = builder.GetStateSnapshot();
                        File.WriteAllText(path, state);

                        break;
                    }

                    case actionLoadFromFile:
                    {
                        var path = Path.GetFullPath(Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lsf-api",
                            "events.json"));
                        Console.Write($"Where should is file stored? [{path}]: ");
                        input = Console.ReadLine();

                        if (!string.IsNullOrEmpty(input)) path = Path.GetFullPath(input);

                        if (!File.Exists(path))
                        {
                            Console.WriteLine("The file does not exist.");
                            break;
                        }

                        var state = File.ReadAllText(path);
                        builder.LoadStateSnapshot(state);

                        break;
                    }
                    case actionGetLoginCookie:
                    {
                        if (!Authenticate(httpClient)) break;

                        var cookie = httpClient.GetLoginCookie();
                        Console.WriteLine(
                            "Here is your session cookie. Treat this as a password as anybody can access your account using this cookie until you log out of the session.");
                        Console.WriteLine(cookie);

                        break;
                    }
                    default:
                    {
                        Console.WriteLine("Unrecognized input");
                        break;
                    }
                }
            }
        }

        private static bool Authenticate(LsfHttpClient httpClient)
        {
            Console.Write("Please enter your lsf username: ");
            var userName = Console.ReadLine();

            Console.Write("Please enter your lsf password: ");
            var password = ReadPassword();

            var success = httpClient.Authenticate(userName, password).Result;


            if (!success)
            {
                Console.WriteLine("Authentication failed!");
                return false;
            }

            return true;
        }

        private static string ReadPassword()
        {
            var pass = "";
            do
            {
                var key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, pass.Length - 1);
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);

            Console.WriteLine();

            return pass;
        }
    }
}