using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lsf.Client;
using Lsf.Grading.Models;
using Lsf.Grading.Parser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lsf.Grading.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Config _config;

        private readonly List<INotifier> _notifiers;
        private readonly LsfHttpClient _httpClient;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config.Get<Config>();
            _notifiers = new List<INotifier>
            {
                new TelegramNotifier(_config.TelegramBotAccessToken, _logger, _config.TelegramChatId)
            };
            _httpClient = new LsfHttpClientImpl(_config.BaseUrl);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!await _httpClient.Authenticate(_config.LoginCookie))
            {
                await HandleError("Authentication for fetching grades failed!");
                return;
            }
            
            _logger.LogDebug($"Execution started");

            
            var parser = new GradingParser(_httpClient);
            IEnumerable<Degree> degrees = new List<Degree>();
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    degrees = await UpdateExamResults(parser, degrees);
                }
                catch (Exception e)
                {
                    await HandleError("An Exception occured: \n" + e);
                    throw;
                }

                await Task.Delay(15 * 60 * 1000, stoppingToken);
            }
        }

        private async Task HandleError(string message)
        {
            _logger.LogError(message);
            await Notify(message);
        }

        private async Task<IEnumerable<Degree>> UpdateExamResults(GradingParser parser, IEnumerable<Degree> previousDegrees)
        {
            var changes = new List<ExamResultChangeTracking>();
            var currentDegrees = await parser.GetGradesForAllDegrees();

            var previousDegreesArr = previousDegrees as Degree[] ?? previousDegrees.ToArray();
            foreach (var degree in currentDegrees)
            {
                if (!previousDegreesArr.Contains(degree, new GenericEqualityComparer<Degree>((a, b) => a.Id == b.Id)))
                {
                    changes.AddRange(ToExamResultChangeTracking(degree));
                }
                else
                {
                    foreach (var major in degree.GradingMajors)
                    {
                        if (!degree.GradingMajors.Contains(major,
                            new GenericEqualityComparer<Major>((a, b) => a.Id == b.Id)))
                        {
                            changes.AddRange(ToExamResultChangeTracking(degree, major));
                        }
                        else
                        {
                            changes.AddRange(major.Gradings.Where(grading => !major.Gradings.Contains(grading,
                                    new GenericEqualityComparer<ExamResult>((a, b) =>
                                        a.ExamNumber == b.ExamNumber && a.Try == b.Try &&
                                        a.ExamState == b.ExamState &&
                                        (float.IsNaN(a.Grade) && float.IsNaN(b.Grade) ||
                                         Math.Abs(a.Grade - b.Grade) < .001))))
                                .Select(grading => ToExamResultChangeTracking(degree, major, grading)));
                        }
                    }
                }
            }

            var changedDegrees = changes
                .GroupBy(x => x.DegreeId)
                .Select(g => new Degree
                {
                    Id = g.Key,
                    Name = g.First().DegreeName,
                    GradingMajors = g.GroupBy(x => x.MajorId).Select(mg =>
                        new Major
                        {
                            Id = mg.Key,
                            Name = mg.First().MajorName,
                            Gradings = mg.Select(x => x.ExamResult)
                        })
                }).ToArray();

            if (changedDegrees.Length > 0)
            {
                _logger.LogInformation($"Found {changes.Count} changes in exam results.");
                await Notify(changedDegrees);
            }
            else
            {
                _logger.LogDebug($"Found no new exams");
            }

            return previousDegreesArr.Union(changedDegrees, new GenericEqualityComparer<Degree>((a, b) => a.Id == b.Id));
        }


        private Task Notify(IEnumerable<Degree> degrees) => Task.WhenAll(_notifiers.Select(n => n.NotifyChange(degrees)));

        private Task Notify(string message) => Task.WhenAll(_notifiers.Select(n => n.NotifyError(message)));

        private IEnumerable<ExamResultChangeTracking> ToExamResultChangeTracking(Degree degree) =>
            degree.GradingMajors.SelectMany(m => ToExamResultChangeTracking(degree, m));

        private IEnumerable<ExamResultChangeTracking> ToExamResultChangeTracking(Degree degree, Major major) =>
            major.Gradings.Select(g => ToExamResultChangeTracking(degree, major, g));
        private ExamResultChangeTracking ToExamResultChangeTracking(Degree degree, Major major, ExamResult result) =>
            new ExamResultChangeTracking
            {
                DegreeId = degree.Id,
                DegreeName = degree.Name,
                ExamResult = result,
                MajorId = major.Id,
                MajorName = major.Name
            };
    }
}