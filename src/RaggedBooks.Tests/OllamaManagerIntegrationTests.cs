using Microsoft.Extensions.Logging;
using RaggedBooks.Core;
using RaggedBooks.Core.Configuration;

namespace RaggedBooks.Tests;

public class OllamaManagerIntegrationTests
{
    [Test]
    public async Task PullOllamaModel()
    {
        var modelId = "nomic-embed-text";
        var config = new RaggedBookConfig() { OllamaUrl = new Uri("http://localhost:11434") };
        var ollamaModelManager = new OllamaModelManager(
            config,
            new Logger<OllamaModelManager>(new LoggerFactory())
        );
        await ollamaModelManager.PullModel(modelId);
        Assert.Pass();
    }

    [Test]
    public async Task GetOllamaModels()
    {
        var config = new RaggedBookConfig() { OllamaUrl = new Uri("http://localhost:11434") };
        var ollamaModelManager = new OllamaModelManager(
            config,
            new Logger<OllamaModelManager>(new LoggerFactory())
        );
        var models = await ollamaModelManager.GetModels();
        Assert.That(models, Is.Not.Null);
        Assert.That(models.Length, Is.GreaterThan(0));
    }
}
