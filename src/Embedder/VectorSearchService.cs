using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
#pragma warning disable SKEXP0001

namespace Embedder;

public class VectorSearchService
{
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
    private readonly IVectorStoreRecordCollection<ulong, ContentChunk> _collection;

    public VectorSearchService(Kernel kernel)
    {
        _textEmbeddingGenerationService =
            kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        // Create a Qdrant VectorStore object
        var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));

        _collection = vectorStore.GetCollection<ulong, ContentChunk>("skcontent");
    }

    public async Task UpsertItems(ContentChunk[] items)
    {
        var collection = await GetCollection();

        var keys = new List<ulong>();
        Console.WriteLine("Adding records to Qdrant");
        await foreach (var key in collection.UpsertBatchAsync(items))
        {
            keys.Add(key);
        }
        Console.WriteLine($"Added {keys.Count} records to Qdrant");
    }

    public async Task<IVectorStoreRecordCollection<ulong, ContentChunk>> GetCollection()
    {
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
            new VectorSearchOptions { Top = 5, IncludeVectors = false }
        );
        return searchResult;
    }
}
