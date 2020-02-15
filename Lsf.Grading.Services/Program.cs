using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                // .ConfigureAppConfiguration(((hostingContext, config) => { config.AddJsonFile("appsettings.json", ); }))
                .ConfigureServices((hostContext, services) =>
                {
                    
                    services.AddHostedService<Worker>();
                });
    }
}