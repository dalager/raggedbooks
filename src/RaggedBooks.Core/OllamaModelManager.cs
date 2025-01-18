using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace RaggedBooks.Core;

/// <summary>
/// Manages the Ollama models.
/// Used for pulling models into ollama.
/// This is convenient as it allows the user to
/// add models to ollama through the configuration file
/// </summary>
public class OllamaModelManager
{
    private readonly RaggedBookConfig _config;
    private readonly ILogger<OllamaModelManager> _logger;

    public OllamaModelManager(RaggedBookConfig config, ILogger<OllamaModelManager> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task PullRequiredModels(Action<string> updateStatus)
    {
        var loadedModels = await GetModels();
        if (!string.IsNullOrWhiteSpace(_config.EmbeddingModel))
        {
            _logger.LogInformation("Pulling embedding model into ollama");
            updateStatus(_config.EmbeddingModel);
            if (!loadedModels.Contains(_config.EmbeddingModel))
            {
                await PullModel(_config.EmbeddingModel);
            }
        }

        if (_config.UseLocalChatModel)
        {
            _logger.LogInformation("Pulling chatcompletionmodel into ollama");
            updateStatus(_config.ChatCompletionModel);
            if (!loadedModels.Contains(_config.ChatCompletionModel))
            {
                await PullModel(_config.ChatCompletionModel);
            }
            await PullModel(_config.ChatCompletionModel);
        }
    }

    public async Task PullModel(string modelId)
    {
        try
        {
            var httpclient = new HttpClient();
            var payload = new { model = modelId };
            var requestUri = $"{_config.OllamaUrl}api/pull";
            _logger.LogInformation(
                "Pulling model {ModelId} into ollama on {Url}",
                modelId,
                requestUri
            );
            var response = await httpclient.PostAsJsonAsync(requestUri, payload);

            var body = await response.Content.ReadAsStringAsync();

            // check lines in output for errors
            var lines = body.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("\"error\""))
                {
                    throw new OllamaException(line);
                }
            }

            _logger.LogInformation("Response: {Response}", body);

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
            $"{_config.OllamaUrl}api/tags"
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

public class OllamaException : Exception
{
    public OllamaException(string message)
        : base(message) { }
}
