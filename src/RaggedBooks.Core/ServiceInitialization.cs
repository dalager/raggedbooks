using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using RaggedBooks.Core.Chat;
using RaggedBooks.Core.Configuration;
using RaggedBooks.Core.SemanticSearch;
using RaggedBooks.Core.TextExtraction;
using Serilog;

#pragma warning disable SKEXP0070

namespace RaggedBooks.Core
{
    public static class ServiceInitialization
    {
        public static ServiceCollection CreateServices(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    "raggedbooks_log.log",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            var services = new ServiceCollection();
            services.AddLogging(l =>
            {
                l.AddSerilog(dispose: true);
            });

            var raggedBookConfig = new RaggedBookConfig();
            configuration.GetSection("AppSettings").Bind(raggedBookConfig);
            raggedBookConfig.ValidateConfiguration();
            services.AddSingleton(raggedBookConfig);

            services.AddSingleton<OllamaModelManager>();
            services.AddSingleton<IChatService,ChatService>();
            services.AddSingleton<IRagService, RagService>();
            
            // vector store and api
            services.AddQdrantVectorStore();
            services.AddSingleton<IVectorSearchService,VectorSearchService>();
            services.AddSingleton<QDrantApiClient>();

            services.AddSingleton<FileImportService>();
            services.AddSingleton<Kernel>(serviceProvider =>
            {
                var azureOpenAiSettings = serviceProvider
                    .GetRequiredService<IOptions<AzureOpenAiSettings>>()
                    .Value;
                var kernelBuilder = Kernel.CreateBuilder();

                if (!raggedBookConfig.UseLocalChatModel)
                {
                    var apiKey = azureOpenAiSettings.ApiKey;
                    var azureEndpoint = azureOpenAiSettings.Endpoint;

                    kernelBuilder.AddAzureOpenAIChatCompletion(
                        azureOpenAiSettings.ModelId,
                        azureEndpoint,
                        apiKey
                    );
                }
                else
                {
                    kernelBuilder.AddOllamaChatCompletion(
                        raggedBookConfig.ChatCompletionModel,
                        raggedBookConfig.OllamaUrl
                    );
                }
                kernelBuilder.AddOllamaTextEmbeddingGeneration(
                    raggedBookConfig.EmbeddingModel,
                    raggedBookConfig.OllamaUrl
                );
                var kernel = kernelBuilder.Build();

                return kernel;
            });

            services.Configure<AzureOpenAiSettings>(options =>
                configuration.GetSection("AzureOpenAiSettings").Bind(options)
            );

            return services;
        }
    }
}
