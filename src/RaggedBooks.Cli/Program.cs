#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaggedBooks.Core;

namespace RaggedBooks.Cli;

internal class Program
{
    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var configFolder = new DirectoryInfo(Path.Combine(@"..\Config\"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                Path.Combine(configFolder.FullName, "Appsettings.json"),
                optional: false,
                reloadOnChange: true
            )
            .Build();

        var serviceCollection = ServiceInitialization.CreateServices(configuration);
        serviceCollection.AddSingleton<RaggedBooksCli>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var cli = serviceProvider.GetRequiredService<RaggedBooksCli>();
        await cli.Run(args);
    }
}
