using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lsf.Models;
using Lsf.Parser;
using Lsf.Schedule.Criteria;
using Newtonsoft.Json;

namespace Lsf.Schedule
{
    public class ScheduleBuilder<T, S> where T: Schedule<S>, new() where S:ScheduleItem
    {
        private readonly Dictionary<string, EventEntry> _eventEntries = new Dictionary<string, EventEntry>();
        private readonly List<IItemCriterion> _itemCriteria = new List<IItemCriterion>();
        private readonly List<IScheduleCriterion> _scheduleCriteria = new List<IScheduleCriterion>();
        private readonly EventParser _eventParser;
        private readonly IScheduleItemFactory<S> _factory;

        public ScheduleBuilder(string baseUrl, IScheduleItemFactory<S> factory)
        {
            _factory = factory;
            _eventParser = new EventParser(baseUrl);
        }

        public ScheduleBuilder(EventParser eventParser, IScheduleItemFactory<S> factory)
        {
            _eventParser = eventParser;
            _factory = factory;
        }

        public int EventsCount => _eventEntries.Count;
        public bool IsBuild { get; private set; } = false;

        public void AddScheduleCriterion(IScheduleCriterion criterion)
        {
            if (!criterion.MultipleCriteriaAllowed && _scheduleCriteria.Any(c => c.GetType() == criterion.GetType()))
            {
                throw new InvalidOperationException("The criterion only allows to be applied once");
            }
            
            _scheduleCriteria.Add(criterion);
        }

        public void AddItemCriterion(IItemCriterion criterion)
        {
            if (!criterion.MultipleCriteriaAllowed && _itemCriteria.Any(c => c.GetType() == criterion.GetType()))
            {
                throw new InvalidOperationException("The criterion only allows to be applied once");
            }
            
            _itemCriteria.Add(criterion);
        }

        public void RemoveItemCriterion(Type criterionType)
        {
            _itemCriteria.RemoveAll(criterionType.IsInstanceOfType);
        }
        public void RemoveItemCriterion(IItemCriterion criterion)
        {
            if(_itemCriteria.Contains(criterion))
            {
                _itemCriteria.Remove(criterion);
            }
        }

        public void RemoveScheduleCriterion(IScheduleCriterion criterion)
        {
            if(_scheduleCriteria.Contains(criterion))
            {
                _scheduleCriteria.Remove(criterion);
            }
        }
        public void RemoveScheduleCriterion(Type criterionType)
        {
            _scheduleCriteria.RemoveAll(criterionType.IsInstanceOfType);
        }

        public async Task<T[]> Build()
        {
            Console.Write($"Loading {_eventEntries.Count} events ...");
            var events = await LoadEvents();
            Console.WriteLine(" Done");

            var schedules = new List<T>();

            var draftSchedule = new Func<List<S>>(() => new List<S>(events.SelectMany(e => _eventEntries[e.EventId]
                .GetFixedScheduleItems(e, _factory))));

            var smallGroupEvents = events.Where(e => e.HasSmallGroup && !_eventEntries[e.EventId].HasFixedSmallGroup).ToArray();

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

                    schedule.AddRange(sg.Appointments.Select(a => _factory.Create(a, sg)));
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
            
            IsBuild = true;
            
            return result.OrderByDescending(x => x.Rating).ToArray();
        }

        private static int GetSmallGroupIndexForEvent(int globalIndex, int eventIndex, int[] lengths)
        {
            return globalIndex / lengths.Take(eventIndex).Aggregate(1, (prod, length) => prod * length) %
                   lengths[eventIndex];
        }

        private Task<Event[]> LoadEvents()
        {
            return Task.WhenAll(_eventEntries.Values.Select(entry => _eventParser.Parse(entry.EventId)));
        }

        public EventEntryBuilder AddEvent(string eventId)
        {
            var entry = new EventEntry { EventId = eventId};
            
            _eventEntries.Add(eventId, entry);
            
            return new EventEntryBuilder(entry);
        }
        
        internal  class EventEntry
        {
            public string EventId { get; set; }
            public IList<string> FixedSmallGroupNames { get;  } = new List<string>();

            public IEnumerable<S> GetFixedScheduleItems(Event e, IScheduleItemFactory<S> factory)
            {
                return e.HasSmallGroup
                    ? e.SmallGroups.Where(sg => FixedSmallGroupNames.Contains(sg.Name)).SelectMany(g => g.Appointments.Select(a => factory.Create(a, g)))
                    : e.Appointments.Select(a => factory.Create(a, e));
            }
            
            public bool HasFixedSmallGroup => FixedSmallGroupNames.Count > 0;
        }
        

        public class EventEntryBuilder
        {
            internal EventEntryBuilder(EventEntry entry)
            {
                _entry = entry;
            }

            private readonly EventEntry _entry;
        
            public EventEntryBuilder WithFixedSmallGroup(string smallGroupNumber)
            {
                _entry.FixedSmallGroupNames.Add(smallGroupNumber);
                return this;
            }
        }

        public string GetStateSnapshot()
        {
            return JsonConvert.SerializeObject(new
                {events = _eventEntries, itemCriteria = _itemCriteria, scheduleCriteria = _scheduleCriteria}, new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.Auto});
        }

        public void LoadStateSnapshot(string stateSnapshot)
        {
            var obj = JsonConvert.DeserializeAnonymousType(stateSnapshot, new
                {events = _eventEntries, itemCriteria = _itemCriteria, scheduleCriteria = _scheduleCriteria}, new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.Auto});

            _eventEntries.Clear();
            foreach (var (key, value) in obj.events)
            {
                _eventEntries.Add(key, value);
            }
            
            _itemCriteria.Clear();
            _itemCriteria.AddRange(obj.itemCriteria);
            
            _scheduleCriteria.Clear();
            _scheduleCriteria.AddRange(obj.scheduleCriteria);

        }
    }
}