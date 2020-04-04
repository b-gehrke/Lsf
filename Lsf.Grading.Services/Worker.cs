using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lsf.Client;
using Lsf.Grading.Models;
using Lsf.Grading.Parser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lsf.Grading.Services
{
    public class Worker : BackgroundService
    {
        private readonly Config _config;
        private readonly LsfHttpClient _httpClient;
        private readonly IHostApplicationLifetime _lifeTime;
        private readonly ILogger<Worker> _logger;

        private readonly List<INotifier> _notifiers;

        public Worker(ILogger<Worker> logger, IConfiguration config, IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _config = config.Get<Config>();
            _lifeTime = lifeTime;
            _notifiers = new List<INotifier>
            {
                new TelegramNotifier(_config.TelegramBotAccessToken, _logger, _config.TelegramChatId)
            };
            _httpClient = new LsfHttpClientImpl(_config.BaseUrl);
        }

        private string SaveFilePath => string.IsNullOrEmpty(_config.SaveFile)
            ? Path.Join(Directory.GetCurrentDirectory(), "gradingresults.json")
            : _config.SaveFile;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task<bool> loginTask;
            if (!string.IsNullOrEmpty(_config.LoginCookie))
            {
                _logger.LogInformation("Logging in with login cookie");
                loginTask = _httpClient.Authenticate(_config.LoginCookie);
            }
            else if (!string.IsNullOrEmpty(_config.Password) && !string.IsNullOrEmpty(_config.UserName))
            {
                _logger.LogInformation("Logging in with username and password");
                loginTask = _httpClient.Authenticate(_config.UserName, _config.Password);
            }
            else
            {
                await HandleError("No authentication provided!");
                return;
            }

            if (!await loginTask)
            {
                await HandleError("Authentication for fetching grades failed!");
                return;
            }

            _logger.LogDebug("Execution started");
            _logger.LogInformation($"Storing and saving results to {SaveFilePath}");


            var parser = new GradingParser(_httpClient);
            var degrees = LoadFromFile().ToArray();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    degrees = (await UpdateExamResults(parser, degrees)).ToArray();
                    SaveToFile(degrees);
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

            _lifeTime.StopApplication();
        }

        private void SaveToFile(IEnumerable<ExamResultChangeTracking> results)
        {
            var json = JsonConvert.SerializeObject(results);
            File.WriteAllText(SaveFilePath, json);
        }

        private IEnumerable<ExamResultChangeTracking> LoadFromFile()
        {
            if (!File.Exists(SaveFilePath)) return Enumerable.Empty<ExamResultChangeTracking>();

            var json = File.ReadAllText(SaveFilePath);
            return JsonConvert.DeserializeObject<ExamResultChangeTracking[]>(json);
        }

        private async Task<IEnumerable<ExamResultChangeTracking>> UpdateExamResults(GradingParser parser,
            IEnumerable<ExamResultChangeTracking> previousExamResults)
        {
            var currentDegrees = await parser.GetGradesForAllDegrees();
            var previousExamResultsArr =
                previousExamResults as ExamResultChangeTracking[] ?? previousExamResults.ToArray();
            var changes = GetChangedExams(previousExamResultsArr, currentDegrees.SelectMany(ToExamResultChangeTracking))
                .ToArray();

            if (changes.Length > 0)
            {
                _logger.LogInformation($"Found {changes.Length} changes in exam results.");
                await Notify(changes);
            }
            else
            {
                _logger.LogDebug("Found no new exams");
            }

            return changes.Union(previousExamResultsArr, new GenericEqualityComparer<ExamResultChangeTracking>((a, b) =>
                a.ExamResult.ExamNumber == b.ExamResult.ExamNumber && a.ExamResult.Try == b.ExamResult.Try &&
                a.ExamResult.ExamState == b.ExamResult.ExamState &&
                (float.IsNaN(a.ExamResult.Grade) && float.IsNaN(b.ExamResult.Grade) ||
                 Math.Abs(a.ExamResult.Grade - b.ExamResult.Grade) < .001)));
        }

        public static IEnumerable<ExamResultChangeTracking> GetChangedExams(
            IEnumerable<ExamResultChangeTracking> previousExamResults,
            IEnumerable<ExamResultChangeTracking> currentExamResults)
        {
            return currentExamResults.Where(grading => !previousExamResults.Contains(grading,
                new GenericEqualityComparer<ExamResultChangeTracking>((a, b) =>
                    a.ExamResult.ExamNumber == b.ExamResult.ExamNumber && a.ExamResult.Try == b.ExamResult.Try &&
                    a.ExamResult.ExamState == b.ExamResult.ExamState &&
                    (float.IsNaN(a.ExamResult.Grade) && float.IsNaN(b.ExamResult.Grade) ||
                     Math.Abs(a.ExamResult.Grade - b.ExamResult.Grade) < .001) &&
                    a.DegreeId == b.DegreeId &&
                    a.DegreeName == b.DegreeName &&
                    a.MajorId == b.MajorId &&
                    a.MajorName == b.MajorName)));
        }

        private Task Notify(IEnumerable<ExamResultChangeTracking> examResults)
        {
            return Notify(examResults.GroupBy(x => x.DegreeId)
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
                }));
        }

        private Task Notify(IEnumerable<Degree> degrees)
        {
            return Task.WhenAll(_notifiers.Select(n => n.NotifyChange(degrees)));
        }

        private Task Notify(string message)
        {
            return Task.WhenAll(_notifiers.Select(n => n.NotifyError(message)));
        }

        private static IEnumerable<ExamResultChangeTracking> ToExamResultChangeTracking(Degree degree)
        {
            return degree.GradingMajors.SelectMany(m => ToExamResultChangeTracking(degree, m));
        }

        private static IEnumerable<ExamResultChangeTracking> ToExamResultChangeTracking(Degree degree, Major major)
        {
            return major.Gradings.Select(g => ToExamResultChangeTracking(degree, major, g));
        }

        private static ExamResultChangeTracking ToExamResultChangeTracking(Degree degree, Major major,
            ExamResult result)
        {
            return new ExamResultChangeTracking
            {
                DegreeId = degree.Id,
                DegreeName = degree.Name,
                ExamResult = result,
                MajorId = major.Id,
                MajorName = major.Name
            };
        }
    }
}