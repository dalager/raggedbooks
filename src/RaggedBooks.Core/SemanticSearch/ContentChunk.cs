namespace RaggedBooks.Core.SemanticSearch;

/// <summary>
/// Chunk stored in vector database.
/// Mapped in VectorSearchService
/// </summary>
public sealed class ContentChunk
{
    public Guid Id { get; set; }

    public required string Book { get; set; }

    public required string BookFilename { get; set; }

    public required string Chapter { get; set; }

    public int PageNumber { get; set; }

    public int Index { get; set; }
    public required string Content { get; set; }

    public ReadOnlyMemory<float> ContentEmbedding { get; set; }
}
