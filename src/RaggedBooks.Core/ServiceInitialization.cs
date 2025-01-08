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
        public static ServiceCollection CreateServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace));
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

            services.Configure<AzureOpenAiSettings>(options =>
                configuration.GetSection("AzureOpenAiSettings").Bind(options)
            );
            services.Configure<RaggedBookConfig>(options =>
                configuration.GetSection("AppSettings").Bind(options)
            );

            return services;
        }
    }
}
