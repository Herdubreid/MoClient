using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Celin
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var loggerFactor = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddConsole();
            });
            ILogger logger = loggerFactor.CreateLogger<Program>();

            AIS.Server server = new AIS.Server(config["baseUrl"], logger);

            var services = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton(logger)
                .AddSingleton(server)
                .BuildServiceProvider();

            var app = new CommandLineApplication<MoCmd>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            try
            {
                app.Execute(args);
            }
            catch (AIS.HttpWebException e)
            {
                logger.LogWarning(e.ErrorResponse.message);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }
    }
}
