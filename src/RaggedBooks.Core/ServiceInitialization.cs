using Embedder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
#pragma warning disable SKEXP0070

namespace RaggedBooks.Core
{
    public static class ServiceInitialization
    {
        public static IServiceProvider CreateServices<T>()
            where T : class
        {
            var services = new ServiceCollection();
            //services.AddLogging(builder => builder.AddConsole());
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

            var configFolder = new DirectoryInfo(Path.Combine(@"..\Config\"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    Path.Combine(configFolder.FullName, "Appsettings.json"),
                    optional: false,
                    reloadOnChange: true
                )
                .Build();

            services.Configure<AzureOpenAiSettings>(options =>
                configuration.GetSection("AzureOpenAiSettings").Bind(options)
            );
            services.Configure<RaggedBookConfig>(options =>
                configuration.GetSection("AppSettings").Bind(options)
            );

            services.AddTransient<T>();

            return services.BuildServiceProvider();
        }
    }
}
