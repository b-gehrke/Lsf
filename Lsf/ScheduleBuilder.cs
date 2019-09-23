using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lsf.Models;
using Lsf.Parser;

namespace Lsf
{
    public class ScheduleBuilder<T, S> where T: Schedule<S>, new() where S:ScheduleItem, new()
    {
        private readonly List<string> _eventIds = new List<string>();
        private readonly List<IItemCriterion> _itemCriteria = new List<IItemCriterion>();
        private readonly List<IScheduleCriterion> _scheduleCriteria = new List<IScheduleCriterion>();
        private readonly EventParser _eventParser;

        public ScheduleBuilder(string baseUrl)
        {
            _eventParser = new EventParser(baseUrl);
        }

        public ScheduleBuilder(EventParser eventParser)
        {
            _eventParser = eventParser;
        } 

        public void AddScheduleCriterion(IScheduleCriterion criterion)
        {
            _scheduleCriteria.Add(criterion);
        }

        public void AddItemCriterion(IItemCriterion criterion)
        {
            _itemCriteria.Add(criterion);
        }

        public async Task<T[]> Build()
        {
            Console.Write($"Loading {_eventIds.Count} events ...");
            var events = await LoadEvents();
            Console.WriteLine(" Done");

            var schedules = new List<T>();

            var fixedEvents = events.Where(e => !e.HasSmallGroup);

            var draftSchedule = new Func<List<S>>(() => new List<S>(fixedEvents.SelectMany(e =>
                e.Appointments.Select(a => new S
                {
                    Appointment = a,
                    ScheduleComponent = e
                }))));

            var smallGroupEvents = events.Where(e => e.HasSmallGroup).ToArray();

            var lengths = smallGroupEvents.Select(e => e.SmallGroups.Length).ToArray();
            var maxIndex = lengths.Aggregate(1, (prod, l) => prod * l);
            Console.Write($"Trying {maxIndex} schedules...");
            for (var i = 0; i < maxIndex; i++)
            {
                var schedule = draftSchedule();

                for (var j = 0; j < smallGroupEvents.Length; j++)
                {
                    var ev = smallGroupEvents[j];
                    var sgi = GetSmallGroupIndexForEvent(i, j, lengths);

                    var sg = ev.SmallGroups[sgi];

                    schedule.AddRange(sg.Appointments.Select(a => new S {Appointment = a, ScheduleComponent = sg}));
                }

                schedules.Add(new T {ScheduleItems = schedule.ToArray()});
            }

            var result = schedules.Where(a => a.Valid()).ToArray();


            foreach (var schedule in result)
            {
                var itemWeight = schedule.Sum(item =>
                    _itemCriteria.Where(criterion => criterion.AppliesTo(item)).Sum(criterion => criterion.Weight));
                var itemRating = Math.Abs(itemWeight) < .00001 ? 0 : schedule.Sum(item =>
                    _itemCriteria.Where(criterion => criterion.AppliesTo(item))
                        .Sum(criterion => criterion.Rate(item) * criterion.Weight)) / itemWeight;

                var scheduleWeight =  _scheduleCriteria.Where(criterion => criterion.AppliesTo(schedule))
                    .Sum(criterion => criterion.Weight);
                var scheduleRating = Math.Abs(scheduleWeight) < .00001 ? 0 : _scheduleCriteria.Where(criterion => criterion.AppliesTo(schedule))
                    .Sum(criterion => criterion.Rate(schedule) * criterion.Weight) / scheduleWeight;

                schedule.Rating = (scheduleRating + itemRating) / 2;
            }
            
            Console.WriteLine($" Found {result.Length} possible schedules");
            return result.OrderByDescending(x => x.Rating).ToArray();
        }

        private static int GetSmallGroupIndexForEvent(int globalIndex, int eventIndex, int[] lengths)
        {
            return globalIndex / lengths.Take(eventIndex).Aggregate(1, (prod, length) => prod * length) %
                   lengths[eventIndex];
        }

        private Task<Event[]> LoadEvents()
        {
            return Task.WhenAll(_eventIds.Select(_eventParser.Parse));
        }

        public void AddEvent(string eventId)
        {
            _eventIds.Add(eventId);
        }
    }
}