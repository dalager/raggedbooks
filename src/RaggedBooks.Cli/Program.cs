#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core;

namespace RaggedBooks.Cli;

public static class Program
{
    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceCollection = ServiceInitialization.CreateServices(configuration);
        serviceCollection.AddLogging(l =>
        {
            l.AddSimpleConsole(opts =>
            {
                opts.SingleLine = true;
                opts.TimestampFormat = "HH:mm:ss ";
            });
        });

        serviceCollection.AddSingleton<RaggedBooksCli>();
        serviceCollection.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace).AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var cli = serviceProvider.GetRequiredService<RaggedBooksCli>();
        await cli.Run(args);
    }
}
