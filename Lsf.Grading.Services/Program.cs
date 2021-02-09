using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lsf.Grading.Services
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configPath = Environment.GetEnvironmentVariable(Constants.ENV_CONFIG_FILE);
            
            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureHostConfiguration(a =>
                {
                    if(!string.IsNullOrEmpty(configPath)) a.AddJsonFile(configPath);
                })
                .ConfigureServices((hostContext, services) => { services.AddHostedService<Worker>(); });
        }
    }
}