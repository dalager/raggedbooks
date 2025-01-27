using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RaggedBooks.Core.Configuration;

namespace RaggedBooks.Core;

/// <summary>
/// Manages the Ollama models.
/// Used for pulling models into ollama.
/// This is convenient as it allows the user to
/// add models to ollama through the configuration file
/// </summary>
public class OllamaModelManager(RaggedBookConfig config, ILogger<OllamaModelManager> logger)
{
    public async Task PullRequiredModels(Action<string> updateStatus)
    {
        var loadedModels = await GetModels();
        if (!string.IsNullOrWhiteSpace(config.EmbeddingModel))
        {
            logger.LogInformation("Pulling embedding model into ollama");
            updateStatus(config.EmbeddingModel);
            if (!loadedModels.Contains(config.EmbeddingModel))
            {
                await PullModel(config.EmbeddingModel);
            }
        }

        if (config.UseLocalChatModel)
        {
            logger.LogInformation("Pulling chatcompletionmodel into ollama");
            updateStatus(config.ChatCompletionModel);
            if (!loadedModels.Contains(config.ChatCompletionModel))
            {
                await PullModel(config.ChatCompletionModel);
            }
            await PullModel(config.ChatCompletionModel);
        }
    }

    public async Task PullModel(string modelId)
    {
        try
        {
            var httpclient = new HttpClient();
            var payload = new { model = modelId };
            var requestUri = $"{config.OllamaUrl}api/pull";
            logger.LogInformation(
                "Pulling model {ModelId} into ollama on {Url}",
                modelId,
                requestUri
            );
            var response = await httpclient.PostAsJsonAsync(requestUri, payload);

            var body = await response.Content.ReadAsStringAsync();

            // check lines in output for errors
            if (body.Contains("\"error\""))
            {
                var lines = body.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                var error = lines.FirstOrDefault(x => x.Contains(@"""error"""));
                if (error != null)
                {
                    throw new OllamaException(error);
                }
            }

            logger.LogInformation("Response: {Response}", body);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new OllamaException(e.Message);
        }
    }

    public async Task<string[]> GetModels()
    {
        var httpclient = new HttpClient();
        var models = await httpclient.GetFromJsonAsync<OllamaTagList>(
            $"{config.OllamaUrl}api/tags"
        );
        if (models == null)
        {
            return [];
        }
        return models.Models.Select(m => m.Name).ToArray();
    }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class OllamaTagList
    {
        public OllamaModel[] Models { get; set; }
    }

    public class OllamaModel
    {
        public string Name { get; set; }
        public string Modified_at { get; set; }
        public long Size { get; set; }
        public string Digest { get; set; }
        public Details Details { get; set; }
    }

    public class Details
    {
        public string Format { get; set; }
        public string Family { get; set; }
        public object Families { get; set; }
        public string Parameter_size { get; set; }
        public string Quantization_level { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

public class OllamaException(string message) : Exception(message);
