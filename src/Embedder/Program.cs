#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Embedder;

internal class Program
{
    //private static readonly AzureOpenAiSettings azureOpenAiSettings = new();
    //private static readonly RaggedBookConfig raggedBookConfig = new();

    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var services = CreateServices();
        var cli = services.GetRequiredService<RaggedBooksCli>();
        await cli.Run(args);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddQdrantVectorStore();
        services.AddSingleton<ChatService>();
        services.AddSingleton<VectorSearchService>();
        services.AddSingleton<FileImportService>();
        services.AddTransient<Kernel>(serviceProvider =>
        {
            var azureOpenAiSettings = serviceProvider
                .GetRequiredService<IOptions<AzureOpenAiSettings>>()
                .Value;
            var raggedBookConfig = serviceProvider
                .GetRequiredService<IOptions<RaggedBookConfig>>()
                .Value;
            var apiKey = azureOpenAiSettings.ApiKey;
            var azureEndpoint = azureOpenAiSettings.Endpoint;
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace));
            kernelBuilder.AddOllamaTextEmbeddingGeneration(
                "nomic-embed-text",
                raggedBookConfig.OllamaUrl
            );
            kernelBuilder.AddAzureOpenAIChatCompletion(
                azureOpenAiSettings.ModelId,
                azureEndpoint,
                apiKey
            );
            var kernel = kernelBuilder.Build();
            return kernel;
        });

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.Configure<AzureOpenAiSettings>(options =>
            configuration.GetSection("AzureOpenAiSettings").Bind(options)
        );
        services.Configure<RaggedBookConfig>(options =>
            configuration.GetSection("AppSettings").Bind(options)
        );

        services.AddTransient<RaggedBooksCli>();

        return services.BuildServiceProvider();
    }
}
