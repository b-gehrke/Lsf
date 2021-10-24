#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lsf.Client;
using Lsf.Grading.Models;
using Lsf.Grading.Parser;
using Lsf.Grading.Services.Notifiers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using static Lsf.Grading.Services.Constants;

namespace Lsf.Grading.Services
{
    public class Worker : BackgroundService
    {
        private readonly Config _config;
        private readonly LsfHttpClient _httpClient;
        private readonly IHostApplicationLifetime _lifeTime;
        private readonly ILogger<Worker> _logger;

        private readonly IList<INotifier> _notifiers;

        public Worker(ILogger<Worker> logger, IConfiguration config, IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _config = config.Get<Config>();
            _lifeTime = lifeTime;
            _notifiers = NotifierFactory.CreateFromConfig(config, logger).ToList();
            _httpClient = new LsfHttpClientImpl(_config.BaseUrl);
        }

        private string SaveFilePath => string.IsNullOrEmpty(_config.SaveFile)
            ? Path.Join(Directory.GetCurrentDirectory(), "gradingresults.json")
            : _config.SaveFile;


        private string? GetPassword()
        {
            return string.IsNullOrEmpty(_config.Password) ? Environment.GetEnvironmentVariable(ENV_LSF_PASSWORD) : _config.Password;
        }

        private string? GetUserName()
        {
            return string.IsNullOrEmpty(_config.UserName) ? Environment.GetEnvironmentVariable(ENV_LSF_USER) : _config.UserName;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task<bool> loginTask;
            if (!string.IsNullOrEmpty(_config.LoginCookie))
            {
                _logger.LogInformation("Logging in with login cookie");
                loginTask = _httpClient.Authenticate(_config.LoginCookie);
            }
            else if (!string.IsNullOrEmpty(GetPassword()) && !string.IsNullOrEmpty(GetUserName()))
            {
                _logger.LogInformation("Logging in with username and password");
                loginTask = _httpClient.Authenticate(GetUserName(), GetPassword());
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
                    var task = UpdateExamResults(parser, degrees);
                    task.Wait(stoppingToken);
                    degrees = task.Result.ToArray();
                    SaveToFile(degrees);
                }
                catch (Exception e)
                {
                    await HandleError(e);
                }

                await Task.Delay(15 * 60 * 1000, stoppingToken);
            }
        }

        private async Task HandleError(Exception e)
        {
            _logger.LogError("An Exception occured: \n" + e);
            await Notify("An Exception occured: \n" + e.Message);
            _lifeTime.StopApplication();
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