using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

#pragma warning disable SKEXP0001

namespace RaggedBooks.Core;

public class VectorSearchService
{
    private readonly ILogger<VectorSearchService> _logger;
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
    private readonly IVectorStoreRecordCollection<ulong, ContentChunk> _collection;

    public VectorSearchService(
        Kernel kernel,
        IOptions<RaggedBookConfig> raggedBookConfig,
        ILogger<VectorSearchService> logger
    )
    {
        _logger = logger;
        _textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        // Create a Qdrant VectorStore object
        var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));

        var vectorStoreRecordDefinition = new VectorStoreRecordDefinition()
        {
            Properties = new List<VectorStoreRecordProperty>()
            {
                new VectorStoreRecordKeyProperty("Id", typeof(Guid)),
                new VectorStoreRecordDataProperty("Book", typeof(string)),
                new VectorStoreRecordDataProperty("BookFilename", typeof(string)),
                new VectorStoreRecordDataProperty("Chapter", typeof(string)),
                new VectorStoreRecordDataProperty("PageNumber", typeof(int)),
                new VectorStoreRecordDataProperty("Index", typeof(int)),
                new VectorStoreRecordDataProperty("Content", typeof(string)),
                new VectorStoreRecordVectorProperty(
                    "ContentEmbedding",
                    typeof(ReadOnlyMemory<float>)
                )
                {
                    Dimensions = raggedBookConfig.Value.EmbeddingDimensions,
                },
            },
        };
        _collection = vectorStore.GetCollection<ulong, ContentChunk>(
            "skcontent",
            vectorStoreRecordDefinition
        );
    }

    public async Task UpsertItems(ContentChunk[] items)
    {
        Console.WriteLine($"Upserting {items.Length} items to Qdrant");
        var collection = await GetCollection();
        var sw = Stopwatch.StartNew();
        var keys = new List<ulong>();
        await foreach (var key in collection.UpsertBatchAsync(items))
        {
            keys.Add(key);
        }
        Console.WriteLine($"Added {keys.Count} records to Qdrant in {sw.ElapsedMilliseconds}ms");
    }

    public async Task<IVectorStoreRecordCollection<ulong, ContentChunk>> GetCollection()
    {
        _logger.LogInformation("Creating collection if not exists");
        // Choose a collection from the database and specify the type of key and record stored in it via Generic parameters.
        await _collection.CreateCollectionIfNotExistsAsync();

        return _collection;
    }

    public async Task<VectorSearchResults<ContentChunk>> SearchVectorStore(string query)
    {
        var searchVector = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(query);
        var collection = await GetCollection();

        var searchResult = await collection.VectorizedSearchAsync(
            searchVector,
            new VectorSearchOptions { Top = 10, IncludeVectors = false }
        );
        return searchResult;
    }
}
